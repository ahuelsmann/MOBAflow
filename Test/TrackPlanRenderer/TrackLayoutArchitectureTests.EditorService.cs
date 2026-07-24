// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.TrackPlanRenderer;

using global::Moba.Backend.Service.TrackPlan;
using global::Moba.SharedUI.Service;
using global::Moba.TrackLibrary.PikoA;

internal sealed partial class TrackLayoutArchitectureTests
{
    [Test]
    public void SelectForDrag_Should_SelectNearestTrackAndConnectedGroup()
    {
        using var service = CreateService(out var plan);
        var first = new PlacedSegment(new G231(), 0, 0, 0);
        var second = new PlacedSegment(new G231(), 230.93, 0, 0);
        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");

        var selection = service.SelectForDrag(115, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selection, Is.Not.Null);
            Assert.That(service.SelectedTrackId, Is.EqualTo(first.Segment.No));
            Assert.That(selection!.MovingGroup, Is.EquivalentTo([first.Segment.No, second.Segment.No]));
        }
    }

    [Test]
    public void PlaceSegment_Should_SnapConnectAndRecordHistory()
    {
        using var service = CreateService(out var plan);
        var target = new PlacedSegment(new G231(), 0, 0, 0);
        plan.AddSegment(target);
        service.MarkClean();
        var moving = new PlacedSegment(new G231(), 230.93, 0, 0);

        service.PlaceSegment(moving, snapEnabled: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plan.Segments, Has.Count.EqualTo(2));
            Assert.That(plan.Connections, Has.Count.EqualTo(1));
            Assert.That(service.CanUndo, Is.True);
            Assert.That(service.IsDirty, Is.True);
        }
    }

    [Test]
    public void CompleteMove_Should_RecordOneGestureAndUndoRigidGroupMove()
    {
        using var service = CreateService(out var plan);
        var first = new PlacedSegment(new G231(), 0, 0, 0);
        var second = new PlacedSegment(new G231(), 230.93, 0, 0);
        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");
        var group = new HashSet<Guid> { first.Segment.No, second.Segment.No };
        service.MarkClean();

        service.BeginGesture();
        service.MoveGroup(group, 50, 10);
        service.MoveGroup(group, 25, 5);
        service.CompleteMove(first.Segment.No, group, snapEnabled: false);
        var undoSucceeded = service.Undo();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(undoSucceeded, Is.True);
            Assert.That(plan.Segments.Single(segment => segment.Segment.No == first.Segment.No).X, Is.Zero);
            Assert.That(plan.Segments.Single(segment => segment.Segment.No == second.Segment.No).X, Is.EqualTo(230.93));
        }
    }

    [Test]
    public void DeleteDisconnectRotateAndFeedback_Should_UseSelectedTrack()
    {
        using var service = CreateService(out var plan);
        var first = new PlacedSegment(new G231(), 0, 0, 0);
        var second = new PlacedSegment(new G231(), 230.93, 0, 0);
        plan.AddSegment(first);
        plan.AddSegment(second);
        plan.AddConnection(first.Segment.No, "PortB", second.Segment.No, "PortA");
        service.Select(first.Segment.No);

        service.DisconnectSelectedTrack();
        service.RotateSelectedTrack(15);
        service.AssignSelectedTrackFeedback(7);
        var updated = service.SelectedTrack;
        service.DeleteSelectedTrack();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated?.RotationDegrees, Is.EqualTo(15));
            Assert.That(updated?.InPort, Is.EqualTo(7));
            Assert.That(plan.Segments.Select(segment => segment.Segment.No), Does.Not.Contain(first.Segment.No));
            Assert.That(service.SelectedTrackId, Is.Null);
        }
    }

    [Test]
    public void ContinuousRotation_Should_CreateOneUndoEntry()
    {
        using var service = CreateService(out var plan);
        var segment = new PlacedSegment(new G231(), 0, 0, 0);
        plan.AddSegment(segment);
        service.Select(segment.Segment.No);
        service.MarkClean();

        service.BeginGesture();
        service.SetSelectedTrackRotation(20);
        service.SetSelectedTrackRotation(35);
        service.CompleteGesture();
        service.Undo();

        Assert.That(plan.Segments.Single().RotationDegrees, Is.Zero);
    }

    [Test]
    public void UndoAndRedo_Should_RestoreDeletedTrack()
    {
        using var service = CreateService(out var plan);
        var segment = new PlacedSegment(new G231(), 0, 0, 0);
        plan.AddSegment(segment);
        service.Select(segment.Segment.No);
        service.MarkClean();

        service.DeleteSelectedTrack();
        var undoSucceeded = service.Undo();
        var redoSucceeded = service.Redo();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(undoSucceeded, Is.True);
            Assert.That(redoSucceeded, Is.True);
            Assert.That(plan.Segments, Is.Empty);
            Assert.That(service.CanUndo, Is.True);
        }
    }

    [Test]
    public void ApplyDocument_Should_ClearSelectionHistoryAndDirtyState_When_MarkedClean()
    {
        using var service = CreateService(out var plan);
        var original = new PlacedSegment(new G231(), 0, 0, 0);
        plan.AddSegment(original);
        service.Select(original.Segment.No);
        service.PlaceSegment(new PlacedSegment(new G231(), 500, 0, 0), snapEnabled: false);
        var replacement = TrackPlanEditorDocument.FromData(
            [new PlacedSegment(new G231(), 900, 0, 0)],
            []);

        service.ApplyDocument(replacement, clearHistory: true, markClean: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plan.Segments.Single().X, Is.EqualTo(900));
            Assert.That(service.SelectedTrackId, Is.Null);
            Assert.That(service.CanUndo, Is.False);
            Assert.That(service.CanRedo, Is.False);
            Assert.That(service.IsDirty, Is.False);
        }
    }

    [Test]
    public void Validate_Should_ReportDisconnectedGroupsAndOpenEnds()
    {
        using var service = CreateService(out var plan);
        plan.AddSegment(new PlacedSegment(new G231(), 0, 0, 0));
        plan.AddSegment(new PlacedSegment(new G231(), 500, 0, 0));

        var result = service.Validate();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Messages, Has.Some.Contains("disconnected groups"));
            Assert.That(result.Messages, Has.Some.Contains("open track ends"));
        }
    }

    [Test]
    public void DirtyState_Should_TransitionAfterMutationAndMarkClean()
    {
        using var service = CreateService(out var plan);
        service.MarkClean();

        plan.AddSegment(new PlacedSegment(new G231(), 0, 0, 0));
        var dirtyAfterMutation = service.IsDirty;
        service.MarkClean();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dirtyAfterMutation, Is.True);
            Assert.That(service.IsDirty, Is.False);
        }
    }

    [Test]
    public void MarkClean_Should_NotClearNewerMutation()
    {
        using var service = CreateService(out var plan);
        plan.AddSegment(new PlacedSegment(new G231(), 0, 0, 0));
        var saveVersion = service.ChangeVersion;
        plan.AddSegment(new PlacedSegment(new G231(), 500, 0, 0));

        service.MarkClean(saveVersion);

        Assert.That(service.IsDirty, Is.True);
    }

    private static TrackPlanEditorService CreateService(out EditableTrackPlan plan)
    {
        plan = new EditableTrackPlan();
        return new TrackPlanEditorService(
            plan,
            new TrackPlanInteractionService(plan),
            new SelectionService(),
            new UndoRedoService<TrackPlanEditorDocument>());
    }
}
