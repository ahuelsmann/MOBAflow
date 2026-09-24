// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using System.ComponentModel;

[TestFixture]
public sealed class EventManagerEventPlanTests
{
    [Test]
    public void SelectingRowIsViewStateOnly()
    {
        using var fixture = new EditorFixture(new Journey
        {
            EventPlan = new JourneyEventPlan { Events = [new(), new() { Count = 3 }] }
        });
        var first = fixture.Editor.Events[0];
        var second = fixture.Editor.Events[1];

        fixture.Editor.SelectedEvent = second;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.IsSelected, Is.False);
            Assert.That(second.IsSelected, Is.True);
            Assert.That(fixture.Editor.Events.Count(item => item.IsSelected), Is.EqualTo(1));
            Assert.That(fixture.Editor.CanEditSelectedEvent, Is.True);
            Assert.That(fixture.ChangeNotifications, Is.Zero);
            Assert.That(fixture.Editor.CanUndo, Is.False);
        }
    }

    [Test]
    public void RefreshAndUndoTransferSelectionToCurrentWrapper()
    {
        using var fixture = new EditorFixture(new Journey
        {
            EventPlan = new JourneyEventPlan { Events = [new() { Count = 2 }, new() { Count = 5 }] }
        });
        var original = fixture.Editor.Events[0];
        fixture.Editor.MoveOrCopyEvent(original, 2, false);
        var moved = fixture.Editor.SelectedEvent!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(original.IsSelected, Is.False);
            Assert.That(moved.IsSelected, Is.True);
            Assert.That(moved.Model.Id, Is.EqualTo(original.Model.Id));
        }

        fixture.Editor.UndoCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(moved.IsSelected, Is.False);
            Assert.That(fixture.Editor.SelectedEvent, Is.SameAs(fixture.Editor.Events[0]));
            Assert.That(fixture.Editor.Events.Count(item => item.IsSelected), Is.EqualTo(1));
            Assert.That(fixture.ChangeNotifications, Is.EqualTo(2));
        }
    }

    [Test]
    public void DeletingLastEventClearsSelectedWrapperAndProperties()
    {
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan { Events = [new()] } });
        var selected = fixture.Editor.SelectedEvent!;

        fixture.Editor.DeleteSelectedEventCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selected.IsSelected, Is.False);
            Assert.That(fixture.Editor.SelectedEvent, Is.Null);
            Assert.That(fixture.Editor.CanEditSelectedEvent, Is.False);
        }
        fixture.Editor.UndoCommand.Execute(null);
        Assert.That(fixture.Editor.SelectedEvent!.IsSelected, Is.True);
    }

    [Test]
    public void MoveEvent_PreservesSparseCountsAndStableIdentifiers()
    {
        using var fixture = new EditorFixture(new Journey
        {
            EventPlan = new JourneyEventPlan
            {
                Events = [new() { Count = 2 }, new() { Count = 3 }, new() { Count = 5 }]
            }
        });
        var source = fixture.Editor.Events[2];
        var id = source.Model.Id;

        fixture.Editor.MoveOrCopyEvent(source, 0, false);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Journey.EventPlan.Events.Select(item => item.Count), Is.EqualTo(new ulong[] { 5, 2, 3 }));
            Assert.That(fixture.Editor.SelectedEvent!.Model.Id, Is.EqualTo(id));
            Assert.That(fixture.Journey.EventPlan.Events, Has.Count.EqualTo(3));
            Assert.That(fixture.ChangeNotifications, Is.EqualTo(1));
        });
    }

    [Test]
    public void CopyEvent_AssignsNewIdentityAndPreservesConditionAndWorkflow()
    {
        var workflow = new Workflow { Name = "Station announcement" };
        using var fixture = new EditorFixture(new Journey
        {
            EventPlan = new JourneyEventPlan
            {
                Events = [new() { InPort = 7, Count = 5, WorkflowId = workflow.Id, Enabled = false }]
            }
        }, workflow);
        var original = fixture.Editor.Events.Single();

        fixture.Editor.MoveOrCopyEvent(original, 1, true);

        var copy = fixture.Journey.EventPlan.Events[1];
        Assert.Multiple(() =>
        {
            Assert.That(copy.Id, Is.Not.EqualTo(original.Model.Id));
            Assert.That(copy.InPort, Is.EqualTo(7));
            Assert.That(copy.Count, Is.EqualTo(5));
            Assert.That(copy.WorkflowId, Is.EqualTo(workflow.Id));
            Assert.That(copy.Enabled, Is.False);
        });
    }

    [Test]
    public void EditingConditionAndWorkflow_PropagatesChangesAndSupportsUndoRedo()
    {
        var workflow = new Workflow { Name = "Signal" };
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan { Events = [new()] } }, workflow);
        fixture.Editor.Events[0].CountText = "5";
        fixture.Editor.Events[0].CommitCountCommand.Execute(null);
        fixture.Editor.Events[0].AssignWorkflowCommand.Execute(fixture.Project.Workflows.Single());

        fixture.Editor.UndoCommand.Execute(null);
        Assert.That(fixture.Editor.Events[0].WorkflowId, Is.Null);
        Assert.That(fixture.Editor.Events[0].Count, Is.EqualTo(5));
        fixture.Editor.UndoCommand.Execute(null);
        Assert.That(fixture.Editor.Events[0].Count, Is.EqualTo(1));
        fixture.Editor.RedoCommand.Execute(null);
        fixture.Editor.RedoCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Journey.EventPlan.Events[0].Count, Is.EqualTo(5));
            Assert.That(fixture.Journey.EventPlan.Events[0].WorkflowId, Is.EqualTo(workflow.Id));
            Assert.That(fixture.ChangeNotifications, Is.EqualTo(6));
        });
    }

    [Test]
    public void ActiveJourney_RemainsEditable()
    {
        using var fixture = new EditorFixture(new Journey
        {
            IsActive = true,
            EventPlan = new JourneyEventPlan { Events = [new() { Count = 2 }] }
        });
        var row = fixture.Editor.Events.Single();

        row.Count = 9;
        fixture.Editor.AddEventCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Editor.CanEdit, Is.True);
            Assert.That(fixture.Journey.EventPlan.Events, Has.Count.EqualTo(2));
            Assert.That(row.Model.Count, Is.EqualTo(9));
            Assert.That(fixture.ChangeNotifications, Is.EqualTo(2));
        });
    }

    [Test]
    public void WorkflowDropCreation_UsesSelectedProjectAndIndependentInPortCounts()
    {
        var workflow = new Workflow { Name = "Arrival" };
        using var fixture = new EditorFixture(new Journey
        {
            EventPlan = new JourneyEventPlan { Events = [new() { InPort = 1, Count = 9 }, new() { InPort = 2, Count = 3 }] }
        }, workflow);
        fixture.Editor.DefaultInPort = 2;
        fixture.Editor.InsertEvent(fixture.Project.Workflows.Single(), 2);
        fixture.Editor.InsertEvent(new ProjectViewModel(new Project { Workflows = [new()] }).Workflows.Single(), 3);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Journey.EventPlan.Events, Has.Count.EqualTo(3));
            Assert.That(fixture.Journey.EventPlan.Events[2].InPort, Is.EqualTo(2));
            Assert.That(fixture.Journey.EventPlan.Events[2].Count, Is.EqualTo(4));
            Assert.That(fixture.Journey.EventPlan.Events[2].WorkflowId, Is.EqualTo(workflow.Id));
        });
    }

    [Test]
    public void CountEditor_PreservesUnsignedIntegerPrecisionAndRejectsInvalidText()
    {
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan { Events = [new()] } });
        var row = fixture.Editor.Events.Single();
        row.CountText = "18446744073709551615";
        row.CommitCountCommand.Execute(null);
        Assert.That(row.Model.Count, Is.EqualTo(ulong.MaxValue));
        row.CountText = "18446744073709551616";
        row.CommitCountCommand.Execute(null);
        Assert.That(row.CountValidationMessage, Is.Not.Empty);
        Assert.That(row.HasCountValidationError, Is.True);
        row.CountText = "0";
        row.CommitCountCommand.Execute(null);
        Assert.That(row.CountValidationMessage, Is.Not.Empty);
        Assert.That(row.Model.Count, Is.EqualTo(ulong.MaxValue));
        row.CountText = "5";
        row.CommitCountCommand.Execute(null);
        Assert.That(row.CountValidationMessage, Is.Empty);
        Assert.That(row.HasCountValidationError, Is.False);
        Assert.That(row.Model.Count, Is.EqualTo(5));
    }

    [Test]
    public void CountDraftCommitsOnceAndCreatesOneUndoStep()
    {
        using var assertions = Assert.EnterMultipleScope();
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan { Events = [new()] } });
        var row = fixture.Editor.Events.Single();
        row.CountText = "2";
        row.CountText = "25";
        row.CountText = "250";
        Assert.That(row.Model.Count, Is.EqualTo(1));
        Assert.That(fixture.ChangeNotifications, Is.Zero);

        row.CommitCountCommand.Execute(null);
        row.CommitCountCommand.Execute(null);
        Assert.That(row.Model.Count, Is.EqualTo(250));
        Assert.That(fixture.ChangeNotifications, Is.EqualTo(1));
        fixture.Editor.UndoCommand.Execute(null);
        Assert.That(fixture.Editor.Events.Single().Count, Is.EqualTo(1));
        Assert.That(fixture.Editor.UndoCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void ChangingSelectionCommitsOnlyThePreviousCountDraft()
    {
        using var assertions = Assert.EnterMultipleScope();
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan { Events = [new(), new() { Count = 3 }] } });
        var first = fixture.Editor.Events[0];
        var second = fixture.Editor.Events[1];
        first.CountText = "25";
        fixture.Editor.SelectedEvent = second;
        Assert.That(first.Count, Is.EqualTo(25));
        Assert.That(second.Count, Is.EqualTo(3));
        Assert.That(fixture.ChangeNotifications, Is.EqualTo(1));
        fixture.Editor.UndoCommand.Execute(null);
        Assert.That(fixture.Editor.Events[0].Count, Is.EqualTo(1));
        Assert.That(fixture.Editor.Events[1].Count, Is.EqualTo(3));
        Assert.That(fixture.Editor.CanUndo, Is.False);
    }

    [Test]
    public void ChangingJourneyCommitsItsPreviousCountDraft()
    {
        using var assertions = Assert.EnterMultipleScope();
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan { Events = [new()] } });
        fixture.Editor.Events[0].CountText = "25";
        fixture.Context.SelectedJourney = new JourneyViewModel(new Journey(), fixture.Project.Model);
        Assert.That(fixture.Journey.EventPlan.Events[0].Count, Is.EqualTo(25));
        Assert.That(fixture.ChangeNotifications, Is.EqualTo(1));
        Assert.That(fixture.Editor.CanUndo, Is.False);
    }

    [Test]
    public void WorkflowSearchDoesNotRefreshEventRowsButRenameDoes()
    {
        using var assertions = Assert.EnterMultipleScope();
        var workflow = new Workflow { Name = "Arrival" };
        using var fixture = new EditorFixture(new Journey
        {
            EventPlan = new JourneyEventPlan { Events = [new() { WorkflowId = workflow.Id }] }
        }, workflow);
        var row = fixture.Editor.Events.Single();
        var notifications = new List<string?>();
        row.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        fixture.Library.SearchText = "Arr";
        fixture.Library.SelectedWorkflow = fixture.Project.Workflows.Single();
        Assert.That(notifications, Is.Empty);

        fixture.Project.Workflows.Single().Name = "Departure";
        Assert.That(notifications, Does.Contain(nameof(JourneyEventViewModel.WorkflowName)));
        Assert.That(row.WorkflowName, Is.EqualTo("Departure"));
        notifications.Clear();
        fixture.Project.Workflows.Clear();
        Assert.That(notifications, Does.Contain(nameof(JourneyEventViewModel.AvailableWorkflows)));
    }

    [Test]
    public void Dispose_UnsubscribesFromSharedSelection()
    {
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan() });
        var original = fixture.Editor.SelectedJourney;
        fixture.Editor.Dispose();
        fixture.Context.SelectedJourney = new JourneyViewModel(new Journey(), fixture.Project.Model);
        Assert.That(fixture.Editor.SelectedJourney, Is.SameAs(original));
    }

    private sealed class EditorFixture : IDisposable
    {
        public EditorFixture(Journey journey, Workflow? workflow = null)
        {
            Journey = journey;
            Project = new ProjectViewModel(new Project { Journeys = [journey], Workflows = workflow == null ? [] : [workflow] });
            JourneyViewModel = Project.Journeys.Single();
            Context = new TestProjectContext(Project, JourneyViewModel);
            Library = new WorkflowLibraryViewModel(Context, null);
            Editor = new EventManagerViewModel(Context, Library);
            JourneyViewModel.PropertyChanged += OnJourneyChanged;
        }

        public Journey Journey { get; }
        public ProjectViewModel Project { get; }
        public JourneyViewModel JourneyViewModel { get; }
        public TestProjectContext Context { get; }
        public WorkflowLibraryViewModel Library { get; }
        public EventManagerViewModel Editor { get; }
        public int ChangeNotifications { get; private set; }

        private void OnJourneyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JourneyViewModel.EventPlan)) ChangeNotifications++;
        }

        public void Dispose()
        {
            JourneyViewModel.PropertyChanged -= OnJourneyChanged;
            Editor.Dispose();
            Library.Dispose();
        }
    }

    private sealed class TestProjectContext(ProjectViewModel project, JourneyViewModel journey) : IProjectContext
    {
        private JourneyViewModel? _journey = journey;
        public event PropertyChangedEventHandler? PropertyChanged;
        public ProjectViewModel? SelectedProject { get; set; } = project;
        public JourneyViewModel? SelectedJourney
        {
            get => _journey;
            set
            {
                _journey = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedJourney)));
            }
        }
        public SolutionViewModel? SolutionViewModel => null;
        public SolutionSaveState SolutionSaveState => SolutionSaveState.Saved;
        public string SolutionSaveStatusText => "Saved";
        public Task SaveSolutionInternalAsync() => Task.CompletedTask;
    }
}
