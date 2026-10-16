// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.TrackPlanRenderer;

using global::Moba.Backend.Service.TrackPlan;
using global::Moba.Common.Configuration;
using global::Moba.Common.Events;
using global::Moba.Domain;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.Service;
using global::Moba.SharedUI.ViewModel;
using global::Moba.TrackLibrary.PikoA;
using global::Moba.TrackPlan.Editor;
using global::Moba.Test.Helpers;
using global::Moba.TrackPlan.Renderer;
using Microsoft.Extensions.Logging;
using Moq;

internal sealed partial class TrackLayoutArchitectureTests
{
    [Test]
    public void PikoATrackCatalog_CreatesSegmentsByCodeWithStableConnectors()
    {
        var catalog = new PikoATrackCatalog();

        bool found = catalog.TryCreateSegment("WR", out var segment);

        Assert.That(found, Is.True);
        Assert.That(catalog.LibraryId, Is.EqualTo(PikoATrackCatalog.Id));
        Assert.That(catalog.GetCode(segment!), Is.EqualTo("WR"));
        Assert.That(catalog.GetPorts(segment!).Select(port => port.Name), Does.Contain("PortC"));
        Assert.That(catalog.TryCreateSegment("unknown", out _), Is.False);
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
            () => document.ToEditableTrackPlanData(TrackPlanTestGeometry.Catalog),
            Throws.InvalidOperationException.With.Message.Contains("marklin-c"));
    }

    [Test]
    public void UndoRedoService_KeepsHistoryOutOfUi()
    {
        var history = new UndoRedoService<string>();
        history.Record("before");

        bool undone = history.TryUndo("after", out var previous);
        bool redone = history.TryRedo(previous, out var next);

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
        using var editorService = new TrackPlanEditorService(
            plan,
            new TrackPlanInteractionService(plan, TrackPlanTestGeometry.PikoA),
            TrackPlanTestGeometry.PikoA,
            new SelectionService(),
            new UndoRedoService<TrackPlanEditorDocument>());
        var viewModel = new TrackPlanViewModel(
            editorService,
            Mock.Of<IDialogService>(),
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
        var service = new TrackPlanInteractionService(plan, TrackPlanTestGeometry.PikoA);

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

        var hit = new TrackPlanInteractionService(plan, TrackPlanTestGeometry.PikoA).HitTest(115, 2);

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
        var service = new TrackPlanInteractionService(plan, TrackPlanTestGeometry.PikoA);

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
        var index = new TrackPlanSpatialIndex(plan, TrackPlanTestGeometry.PikoA);
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
        var index = new TrackPlanSpatialIndex(plan, TrackPlanTestGeometry.PikoA);

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

        var scene = TrackPlanRenderSceneBuilder.Build(TrackPlanTestGeometry.PikoA, [placement]);

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

        var scene = TrackPlanRenderSceneBuilder.Build(TrackPlanTestGeometry.PikoA, [placement], selected, feedback);

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
        var preview = new TrackPlanInteractionService(plan, TrackPlanTestGeometry.PikoA).GetSnapPreview(moving);

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
    public void SpatialIndex_ReturnsLocalCandidates_ForTenThousandTracks()
    {
        var plan = new EditableTrackPlan();
        for (var index = 0; index < 10_000; index++)
            plan.AddSegment(new PlacedSegment(new G231(), index * 250, 0, 0));

        var spatialIndex = new TrackPlanSpatialIndex(plan, TrackPlanTestGeometry.PikoA);
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

        var service = new TrackPlanInteractionService(plan, TrackPlanTestGeometry.PikoA);
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

    [Test]
    public void RendererAssembly_DoesNotReferenceConcretePikoLibrary()
    {
        var references = typeof(PlacedTrackPlanSvgRenderer).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        Assert.That(references, Does.Not.Contain("TrackLibrary.PikoA"));
        Assert.That(references, Does.Contain("TrackLibrary.Base"));
    }

    [Test]
    public void RailroadStateProjector_IsOwnedByBackendAssembly()
    {
        Assert.That(
            typeof(TrackPlanRailroadStateProjector).Assembly,
            Is.EqualTo(typeof(global::Moba.Backend.Z21).Assembly));
    }

    [Test]
    public void EditorSelectionAndHistory_AreOwnedByEditorAssembly()
    {
        var editorAssembly = typeof(EditableTrackPlan).Assembly;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(typeof(SelectionService).Assembly, Is.EqualTo(editorAssembly));
            Assert.That(typeof(UndoRedoService<>).Assembly, Is.EqualTo(editorAssembly));
        }
    }

    [Test]
    public void TrackPlanPage_CodeBehind_RemainsAPlatformAdapter()
    {
        var repositoryRoot = FindRepositoryRoot();
        var codeBehind = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(repositoryRoot, "MOBAflow", "View", "TrackPlanPage.xaml.cs")),
            File.ReadAllText(Path.Combine(repositoryRoot, "MOBAflow", "View", "TrackPlanPage.EditorFeatures.cs")));
        string[] forbiddenPatterns =
        [
            "_plan.AddSegment(",
            "_plan.RemoveSegment(",
            "_plan.UpdateSegmentPosition(",
            "_plan.UpdateSegmentInPort(",
            "_plan.DisconnectSegmentFromGroup(",
            "_plan.AddConnection(",
            "_plan.RemoveConnection(",
            "_plan.MoveGroup(",
            "_plan.LoadFromPlacements(",
            "_plan.HealImplicitConnections(",
            "_interactionService",
            "_undoRedoService",
            "_selectedSegmentId",
            "new ContentDialog",
            "CollectValidationMessages",
            "UndoButton.Click",
            "RedoButton.Click",
            "ValidateButton.Click",
            "DisconnectButton.Click",
            "DeleteButton.Click",
            "RotateLeftButton.Click",
            "RotateRightButton.Click"
        ];

        using (Assert.EnterMultipleScope())
        {
            foreach (var forbiddenPattern in forbiddenPatterns)
            {
                Assert.That(
                    codeBehind,
                    Does.Not.Contain(forbiddenPattern),
                    $"TrackPlanPage code-behind must not own editor behavior matching '{forbiddenPattern}'.");
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the MOBAflow repository root.");
    }
}
