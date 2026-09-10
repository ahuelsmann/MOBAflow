// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Service;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;
using System.ComponentModel;
using System.Text.Json;

[TestFixture]
public sealed class EventManagerEventPlanTests
{
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
            Assert.That(fixture.Journey.EventPlan!.Events.Select(item => item.Count), Is.EqualTo(new ulong[] { 5, 2, 3 }));
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

        var copy = fixture.Journey.EventPlan!.Events[1];
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
            Assert.That(fixture.Journey.EventPlan!.Events[0].Count, Is.EqualTo(5));
            Assert.That(fixture.Journey.EventPlan.Events[0].WorkflowId, Is.EqualTo(workflow.Id));
            Assert.That(fixture.ChangeNotifications, Is.EqualTo(6));
        });
    }

    [Test]
    public async Task LegacyPlan_CannotBeEditedOrConvertedWithoutConfirmation()
    {
        var legacy = new JourneyFeedbackStep { InPort = 4, Index = 10 };
        var dialog = new Mock<IDialogService>();
        dialog.Setup(service => service.ShowConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true)).ReturnsAsync(false);
        using var fixture = new EditorFixture(new Journey { FeedbackSequence = [legacy] }, dialog: dialog.Object);

        fixture.Editor.AddEventCommand.Execute(null);
        await fixture.Editor.CreateEventPlanCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Editor.IsLegacyJourney, Is.True);
            Assert.That(fixture.Editor.CanEdit, Is.False);
            Assert.That(fixture.Journey.EventPlan, Is.Null);
            Assert.That(fixture.Journey.FeedbackSequence.Single(), Is.SameAs(legacy));
            Assert.That(fixture.ChangeNotifications, Is.Zero);
        });
    }

    [Test]
    public async Task CreateEventPlan_KeepsLegacySequenceWithoutReinterpretingRepeatCounts()
    {
        var journey = new Journey { FeedbackSequence = [new() { InPort = 1, Index = 10 }] };
        var original = JsonSerializer.Serialize(journey.FeedbackSequence);
        var dialog = new Mock<IDialogService>();
        dialog.Setup(service => service.ShowConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true)).ReturnsAsync(true);
        using var fixture = new EditorFixture(journey, dialog: dialog.Object);

        await fixture.Editor.CreateEventPlanCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(journey.EventPlan, Is.Not.Null);
            Assert.That(journey.EventPlan!.Events, Is.Empty);
            Assert.That(JsonSerializer.Serialize(journey.FeedbackSequence), Is.EqualTo(original));
            Assert.That(fixture.Editor.CanEdit, Is.True);
            Assert.That(fixture.ChangeNotifications, Is.EqualTo(1));
        });
    }

    [Test]
    public void RunningJourney_BlocksCommandsAndDirectRowEdits()
    {
        using var fixture = new EditorFixture(new Journey { EventPlan = new JourneyEventPlan { Events = [new() { Count = 2 }] } });
        var row = fixture.Editor.Events.Single();
        fixture.JourneyViewModel.UpdateFromSessionState(new JourneySessionState { JourneyId = fixture.Journey.Id, IsActive = true });

        fixture.Editor.AddEventCommand.Execute(null);
        fixture.Editor.DuplicateSelectedEventCommand.Execute(null);
        fixture.Editor.DeleteSelectedEventCommand.Execute(null);
        row.Count = 9;
        row.InPort = 5;

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Editor.CanEdit, Is.False);
            Assert.That(fixture.Editor.AddEventCommand.CanExecute(null), Is.False);
            Assert.That(fixture.Journey.EventPlan!.Events, Has.Count.EqualTo(1));
            Assert.That(row.Model.Count, Is.EqualTo(2));
            Assert.That(row.Model.InPort, Is.EqualTo(1));
            Assert.That(fixture.ChangeNotifications, Is.Zero);
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
            Assert.That(fixture.Journey.EventPlan!.Events, Has.Count.EqualTo(3));
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
        Assert.That(row.Model.Count, Is.EqualTo(ulong.MaxValue));
        row.CountText = "18446744073709551616";
        Assert.That(row.CountValidationMessage, Is.Not.Empty);
        Assert.That(row.HasCountValidationError, Is.True);
        row.CountText = "0";
        Assert.That(row.CountValidationMessage, Is.Not.Empty);
        Assert.That(row.Model.Count, Is.EqualTo(ulong.MaxValue));
        row.CountText = "5";
        Assert.That(row.CountValidationMessage, Is.Empty);
        Assert.That(row.HasCountValidationError, Is.False);
        Assert.That(row.Model.Count, Is.EqualTo(5));
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
        public EditorFixture(Journey journey, Workflow? workflow = null, IDialogService? dialog = null)
        {
            Journey = journey;
            Project = new ProjectViewModel(new Project { Journeys = [journey], Workflows = workflow == null ? [] : [workflow] });
            JourneyViewModel = Project.Journeys.Single();
            Context = new TestProjectContext(Project, JourneyViewModel);
            Library = new WorkflowLibraryViewModel(Context, dialog);
            Editor = new EventManagerViewModel(Context, Library, dialog);
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
