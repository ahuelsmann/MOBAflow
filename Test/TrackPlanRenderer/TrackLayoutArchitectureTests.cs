// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using global::Moba.Backend.Service.TrackPlan;
using global::Moba.Domain;
using global::Moba.TrackLibrary.PikoA;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.ViewModel;
using global::Moba.SharedUI.Service;
using global::Moba.Common.Events;
using global::Moba.TrackPlan.Renderer;
using global::Moba.Common.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

internal sealed class TrackLayoutArchitectureTests
{
    [Test]
    public void PikoATrackLibrary_ExposesStableDefinitionsAndConnectors()
    {
        var library = new PikoATrackLibrary();

        bool found = library.TryGetDefinition("WR", out var definition);

        Assert.That(found, Is.True);
        Assert.That(definition.LibraryId, Is.EqualTo(PikoATrackLibrary.Id));
        Assert.That(definition.Connectors.Select(connector => connector.Id), Does.Contain("PortC"));
    }

    [Test]
    public void TrackPlanDocumentMapper_RoundTripsLegacyDocumentWithPikoDefault()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var legacy = new TrackPlanDocument
        {
            Version = 1,
            LibraryId = string.Empty,
            Segments =
            [
                new TrackPlanSegment { Id = first, Code = "G231", X = 0, Y = 0 },
                new TrackPlanSegment { Id = second, Code = "G231", X = 230.93, Y = 0 }
            ],
            Connections = [new TrackPlanConnection { SourceSegment = first, SourcePort = "PortB", TargetSegment = second, TargetPort = "PortA" }]
        };

        var layout = TrackPlanDocumentMapper.ToLayout(legacy);
        var persisted = TrackPlanDocumentMapper.ToDocument(layout);

        Assert.That(layout.Tracks, Has.Count.EqualTo(2));
        Assert.That(layout.Connections, Has.Count.EqualTo(1));
        Assert.That(persisted.Version, Is.EqualTo(2));
        Assert.That(persisted.LibraryId, Is.EqualTo(PikoATrackLibrary.Id));
    }

    [Test]
    public void EditorDocument_RejectsUnknownInheritedLibrary()
    {
        var document = new TrackPlanEditorDocument
        {
            LibraryId = "marklin-c",
            Segments = [new TrackPlanEditorSegment { Id = Guid.NewGuid(), Code = "G231" }]
        };

        Assert.That(
            () => document.ToEditableTrackPlanData(),
            Throws.InvalidOperationException.With.Message.Contains("marklin-c"));
    }

    [Test]
    public void GraphAndUndoRedoServices_KeepTopologyAndHistoryOutOfUi()
    {
        var first = new TrackInstance(Guid.NewGuid(), "piko-a", "G231", 0, 0, 0);
        var second = new TrackInstance(Guid.NewGuid(), "piko-a", "G231", 230.93, 0, 0);
        var layout = new Layout();
        var layoutService = new LayoutService();
        layoutService.Place(layout, first);
        layoutService.Place(layout, second);
        layoutService.Connect(layout, new Connection(first.Id, "PortB", second.Id, "PortA"));

        var group = new GraphService().GetConnectedGroup(layout, first.Id);
        var history = new UndoRedoService<string>();
        history.Record("before");

        bool undone = history.TryUndo("after", out var previous);
        bool redone = history.TryRedo(previous, out var next);

        Assert.That(group, Is.EquivalentTo(new[] { first.Id, second.Id }));
        Assert.That(undone, Is.True);
        Assert.That(previous, Is.EqualTo("before"));
        Assert.That(redone, Is.True);
        Assert.That(next, Is.EqualTo("after"));
    }

    [Test]
    public void TrackPlanViewModel_MutatesSelectedPlanThroughCommands()
    {
        var plan = new EditableTrackPlan();
        var segment = new PlacedSegment(new G231(), 0, 0, 0);
        plan.AddSegment(segment);
        var settingsService = new Mock<ISettingsService>();
        var viewModel = new TrackPlanViewModel(
            new TrackPlan(),
            plan,
            new SelectionService(),
            new AppSettings(),
            settingsService.Object,
            Mock.Of<ILogger<TrackPlanViewModel>>());

        viewModel.SelectTrack(segment.Segment.No);
        viewModel.RotateSelectedTrack(15);
        viewModel.AssignSelectedTrackFeedback(7);

        var updated = plan.Segments.Single();
        Assert.That(updated.RotationDegrees, Is.EqualTo(15));
        Assert.That(updated.InPort, Is.EqualTo(7));
        Assert.That(viewModel.CanDeleteSelectedTrack, Is.True);

        viewModel.DeleteSelectedTrack();

        Assert.That(plan.Segments, Is.Empty);
        Assert.That(viewModel.SelectedTrackId, Is.Null);
    }

    [Test]
    public void InteractionService_AddWithSnap_CreatesTopologyConnection()
    {
        var plan = new EditableTrackPlan();
        var target = new PlacedSegment(new G231(), 0, 0, 0);
        plan.AddSegment(target);
        var moving = new PlacedSegment(new G231(), 230.93, 0, 0);
        var service = new TrackPlanInteractionService(plan);

        service.AddWithSnap(new TrackPlanSnapHelper.SnapResult(moving, "PortA", target.Segment.No, "PortB", 0));

        Assert.That(plan.Segments, Has.Count.EqualTo(2));
        Assert.That(plan.Connections, Has.Count.EqualTo(1));
    }

    [Test]
    public void InteractionService_HitTest_ReturnsNearestTrackInWorldCoordinates()
    {
        var plan = new EditableTrackPlan();
        var near = new PlacedSegment(new G231(), 0, 0, 0);
        var far = new PlacedSegment(new G231(), 500, 0, 0);
        plan.AddSegment(near);
        plan.AddSegment(far);

        var hit = new TrackPlanInteractionService(plan).HitTest(115, 2);

        Assert.That(hit?.Segment.No, Is.EqualTo(near.Segment.No));
    }

    [Test]
    public void InteractionService_SelectForDrag_ResolvesRigidConnectedGroupAndMovesIt()
    {
        var plan = new EditableTrackPlan();
        var first = new PlacedSegment(new G231(), 0, 0, 0);
        var second = new PlacedSegment(new G231(), 230.93, 0, 0);
        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");
        var service = new TrackPlanInteractionService(plan);

        var selection = service.SelectForDrag(115, 0);
        Assert.That(selection, Is.Not.Null);
        service.MoveGroup(selection!.MovingGroup, 10, 5);

        Assert.Multiple(() =>
        {
            Assert.That(selection.MovingGroup, Is.EquivalentTo([first.Segment.No, second.Segment.No]));
            Assert.That(plan.Segments.Select(segment => segment.X), Is.EquivalentTo([10, 240.93]));
        });
    }

    [Test]
    public void SpatialIndex_RefreshesAfterPlanMutation()
    {
        var plan = new EditableTrackPlan();
        var index = new TrackPlanSpatialIndex(plan);
        var segment = new PlacedSegment(new G231(), 500, 0, 0);

        plan.AddSegment(segment);

        plan.UpdateSegmentPosition(segment.Segment.No, 900, 0, 0);

        Assert.That(index.Query(500, 0, 10), Does.Not.Contain(segment));
        Assert.That(index.Query(900, 0, 10), Does.Contain(plan.Segments.Single()));
    }

    [Test]
    public void SpatialIndex_RemainsAccurateAfterGroupMoveAndConnectionMutation()
    {
        var plan = new EditableTrackPlan();
        var first = new PlacedSegment(new G231(), 0, 0, 0);
        var second = new PlacedSegment(new G231(), 230.93, 0, 0);
        plan.AddSegment(first);
        plan.AddSegment(second);
        var index = new TrackPlanSpatialIndex(plan);

        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");
        plan.MoveGroup(new HashSet<Guid> { first.Segment.No, second.Segment.No }, 500, 0);

        Assert.Multiple(() =>
        {
            Assert.That(index.Query(0, 0, 10), Is.Empty);
            Assert.That(index.Query(500, 0, 10), Does.Contain(plan.Segments.First()));
            Assert.That(index.Query(730.93, 0, 10), Does.Contain(plan.Segments.Last()));
        });
    }

    [Test]
    public void RenderSceneBuilder_ProjectsPlacementWithoutRendererSpecificState()
    {
        var placement = new PlacedSegment(new G231(), 12, 34, 15);

        var scene = TrackPlanRenderSceneBuilder.Build([placement]);

        Assert.That(scene.Items, Has.Count.EqualTo(1));
        Assert.That(scene.Items[0].Id, Is.EqualTo(placement.Segment.No));
        Assert.That(scene.Items[0].X, Is.EqualTo(12));
        Assert.That(scene.Items[0].Path, Is.Not.Empty);
    }

    [Test]
    public void RenderSceneBuilder_ProjectsSelectionFeedbackAndLabelForAllRenderers()
    {
        var placement = new PlacedSegment(new G231(), 12, 34, 15);
        var selected = new HashSet<Guid> { placement.Segment.No };
        var feedback = new Dictionary<Guid, double> { [placement.Segment.No] = 2 };

        var scene = TrackPlanRenderSceneBuilder.Build([placement], selected, feedback);

        Assert.Multiple(() =>
        {
            Assert.That(scene.Items[0].Label, Is.EqualTo(nameof(G231)));
            Assert.That(scene.Items[0].IsSelected, Is.True);
            Assert.That(scene.Items[0].FeedbackIntensity, Is.EqualTo(1));
        });
    }

    [Test]
    public void InteractionService_SnapPreview_ContainsMatchingConnectorPositions()
    {
        var plan = new EditableTrackPlan();
        var target = new PlacedSegment(new G231(), 0, 0, 0);
        var moving = new PlacedSegment(new G231(), 230.93, 0, 0);
        plan.AddSegment(target);
        var preview = new TrackPlanInteractionService(plan).GetSnapPreview(moving);

        Assert.That(preview.HighlightedPorts, Is.Not.Empty);
    }

    [Test]
    public void RailroadStateProjector_ProjectsFeedbackWithoutChangingLayout()
    {
        var plan = new EditableTrackPlan();
        var segment = new PlacedSegment(new G231(), 0, 0, 0, InPort: 8);
        plan.AddSegment(segment);
        var bus = new EventBus(Mock.Of<ILogger<EventBus>>());
        var state = new RailroadState();
        using var projector = new TrackPlanRailroadStateProjector(bus, plan, state);
        projector.Activate();

        bus.Publish(new FeedbackReceivedEvent(8));

        Assert.That(state.IsOccupied(segment.Segment.No), Is.True);
        Assert.That(state.GetLastFeedback(segment.Segment.No), Is.Not.Null);
        Assert.That(plan.Segments, Has.Count.EqualTo(1));
    }

    [Test]
    public void RailroadState_ExpiresFeedbackAndKeepsRuntimeSwitchAndSignalStateOutOfLayout()
    {
        var trackId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var state = new RailroadState();

        state.MarkFeedback(trackId, timestamp);
        state.SetSwitchPosition(11, isLeft: true);
        state.SetSignalAspect(12, "Proceed");
        state.ExpireFeedback(timestamp.AddSeconds(2), TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
            Assert.That(state.GetSwitchPosition(11), Is.True);
            Assert.That(state.GetSignalAspect(12), Is.EqualTo("Proceed"));
        });
    }

    [Test]
    public void RailroadStateProjector_ProjectsSwitchAndSignalEvents()
    {
        var bus = new EventBus(Mock.Of<ILogger<EventBus>>());
        var state = new RailroadState();
        using var projector = new TrackPlanRailroadStateProjector(bus, Mock.Of<ITrackFeedbackLookup>(), state);
        projector.Activate();

        bus.Publish(new SwitchPositionChangedEvent(4, isLeft: false));
        bus.Publish(new SignalAspectChangedEvent(5, "Stop"));

        Assert.Multiple(() =>
        {
            Assert.That(state.GetSwitchPosition(4), Is.False);
            Assert.That(state.GetSignalAspect(5), Is.EqualTo("Stop"));
        });
    }

    [Test]
    public void RailroadStateProjector_ExpiresFeedbackUsingConfiguredTimeout()
    {
        var plan = new EditableTrackPlan();
        var segment = new PlacedSegment(new G231(), 0, 0, 0, InPort: 8);
        plan.AddSegment(segment);
        var bus = new EventBus(Mock.Of<ILogger<EventBus>>());
        var state = new RailroadState();
        using var projector = new TrackPlanRailroadStateProjector(bus, plan, state)
        {
            FeedbackTimeout = TimeSpan.Zero
        };
        projector.Activate();

        bus.Publish(new FeedbackReceivedEvent(8));
        projector.ExpireFeedbackNow();

        Assert.That(state.IsOccupied(segment.Segment.No), Is.False);
    }

    [Test]
    public void TrackLibraryRegistry_ResolvesDefinitionsByPersistedLibraryId()
    {
        var registry = new TrackLibraryRegistry([new PikoATrackLibrary()]);

        var definition = registry.ResolveDefinition(PikoATrackLibrary.Id, "G231");

        Assert.That(definition.TemplateId, Is.EqualTo("G231"));
        Assert.That(() => registry.ResolveDefinition("missing", "G231"), Throws.InvalidOperationException);
    }

    [Test]
    public void SpatialIndex_ReturnsLocalCandidates_ForTenThousandTracks()
    {
        var plan = new EditableTrackPlan();
        for (var index = 0; index < 10_000; index++)
            plan.AddSegment(new PlacedSegment(new G231(), index * 250, 0, 0));

        var spatialIndex = new TrackPlanSpatialIndex(plan);
        var candidates = spatialIndex.Query(1_250_000, 0, 30);

        Assert.That(candidates, Is.Not.Empty);
        Assert.That(candidates.Count, Is.LessThan(10));
    }

    [Test]
    public void InteractionService_UpdatesAndQueriesLocalCandidates_ForTenThousandTracks()
    {
        var plan = new EditableTrackPlan();
        for (var index = 0; index < 10_000; index++)
            plan.AddSegment(new PlacedSegment(new G231(), index * 250, 0, 0));

        var service = new TrackPlanInteractionService(plan);
        var selection = service.SelectForDrag(1_250_115, 0);
        Assert.That(selection, Is.Not.Null);

        service.MoveGroup(selection!.MovingGroup, 100, 0);
        var moved = service.HitTest(1_250_215, 0);

        Assert.That(moved?.Segment.No, Is.EqualTo(selection.SelectedSegmentId));
    }
    [Test]
    public void RailroadState_ConcurrentFeedbackAndExpiry_RemainsConsistent()
    {
        var state = new RailroadState();
        var trackIds = Enumerable.Range(0, 32).Select(_ => Guid.NewGuid()).ToArray();
        var start = DateTimeOffset.UtcNow;

        Assert.DoesNotThrow(() => Parallel.For(0, 10_000, index =>
        {
            var trackId = trackIds[index % trackIds.Length];
            if ((index & 1) == 0)
                state.MarkFeedback(trackId, start.AddTicks(index));
            else
                state.ExpireFeedback(start.AddSeconds(1), TimeSpan.Zero);
        }));

        foreach (var trackId in trackIds)
            state.MarkFeedback(trackId, start.AddSeconds(2));

        Assert.That(trackIds.All(state.IsOccupied), Is.True);
    }

    [Test]
    public void RailroadStateProjector_DisposeIsSafeDuringConcurrentFeedbackAndExpiry()
    {
        var plan = new EditableTrackPlan();
        var segment = new PlacedSegment(new G231(), 0, 0, 0, InPort: 8);
        plan.AddSegment(segment);
        var bus = new EventBus(Mock.Of<ILogger<EventBus>>());
        var state = new RailroadState();
        var projector = new TrackPlanRailroadStateProjector(bus, plan, state)
        {
            FeedbackTimeout = TimeSpan.Zero
        };
        projector.Activate();

        Assert.DoesNotThrow(() => Parallel.Invoke(
            () => Parallel.For(0, 1_000, _ => bus.Publish(new FeedbackReceivedEvent(8))),
            () => Parallel.For(0, 1_000, _ => projector.ExpireFeedbackNow()),
            projector.Dispose));

        projector.Dispose();
    }

    [Test]
    public void RailroadStateProjector_DisposePreventsFutureFeedbackAndExpiry()
    {
        var plan = new EditableTrackPlan();
        var segment = new PlacedSegment(new G231(), 0, 0, 0, InPort: 8);
        plan.AddSegment(segment);
        var bus = new EventBus(Mock.Of<ILogger<EventBus>>());
        var state = new RailroadState();
        var projector = new TrackPlanRailroadStateProjector(bus, plan, state)
        {
            FeedbackTimeout = TimeSpan.Zero
        };
        projector.Activate();
        projector.Dispose();

        state.MarkFeedback(segment.Segment.No, DateTimeOffset.UtcNow);
        projector.ExpireFeedbackNow();
        var occupiedAfterExpiryAttempt = state.IsOccupied(segment.Segment.No);

        state.ClearFeedback(segment.Segment.No);
        bus.Publish(new FeedbackReceivedEvent(8));

        Assert.Multiple(() =>
        {
            Assert.That(occupiedAfterExpiryAttempt, Is.True);
            Assert.That(state.IsOccupied(segment.Segment.No), Is.False);
        });
    }

}
