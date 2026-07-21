// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.WorkflowSteps;

using System.ComponentModel;
using System.Text.Json;

/// <summary>Verifies shared workflow catalog identity, graph operations, references, and save coordination.</summary>
[TestFixture]
public sealed class WorkflowLibraryViewModelTests
{
    [Test]
    public async Task CreateWorkflowCommand_AddsValidMinimalGraphThroughProjectWrapper()
    {
        var projectViewModel = new ProjectViewModel(new Project());
        var context = new TestProjectContext(projectViewModel);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));

        await library.CreateWorkflowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(projectViewModel.Model.Workflows, Has.Count.EqualTo(1));
            Assert.That(projectViewModel.Workflows, Has.Count.EqualTo(1));
            Assert.That(library.SelectedWorkflow, Is.SameAs(projectViewModel.Workflows.Single()));
            Assert.That(library.SelectedWorkflow!.Model.EntryStepId, Is.EqualTo(library.SelectedWorkflow.Model.Steps!.Single().Id));
            Assert.That(library.SelectedWorkflow.Steps.Single(), Is.TypeOf<WorkflowTerminateStepViewModel>());
            Assert.That(context.SaveCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DuplicateSelectedWorkflowCommand_RemapsInternalGraphAndActionIdentifiers()
    {
        var action = new WorkflowActionStep
        {
            Name = "Action",
            Action = new WorkflowAction
            {
                Name = "Command",
                Type = ActionType.Command,
                Command = new CommandActionPayload { BytesBase64 = "AA==" }
            }
        };
        var terminate = new WorkflowTerminateStep { Name = "Done" };
        action.NextStepId = terminate.Id;
        var source = new Workflow
        {
            Name = "Source",
            EntryStepId = action.Id,
            Steps = [action, terminate]
        };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [source] });
        var context = new TestProjectContext(projectViewModel);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));

        await library.DuplicateSelectedWorkflowCommand.ExecuteAsync(null);

        var duplicate = library.SelectedWorkflow!.Model;
        var duplicateAction = (WorkflowActionStep)duplicate.Steps![0];
        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Id, Is.Not.EqualTo(source.Id));
            Assert.That(duplicate.Steps.Select(step => step.Id), Is.All.Not.EqualTo(action.Id).And.Not.EqualTo(terminate.Id));
            Assert.That(duplicate.EntryStepId, Is.EqualTo(duplicateAction.Id));
            Assert.That(duplicateAction.NextStepId, Is.EqualTo(duplicate.Steps[1].Id));
            Assert.That(duplicateAction.Action!.Id, Is.Not.EqualTo(action.Action!.Id));
            Assert.That(projectViewModel.Workflows, Has.Count.EqualTo(2));
            Assert.That(context.SaveCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DeleteSelectedWorkflowCommand_BlocksAndReportsEveryReference()
    {
        var target = new Workflow { Name = "Target", Steps = [] };
        var nested = new WorkflowNestedStep { Name = "Call target", WorkflowId = target.Id };
        var caller = new Workflow { Name = "Caller", EntryStepId = nested.Id, Steps = [nested] };
        var journey = new Journey
        {
            Name = "Regional",
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 1, WorkflowId = target.Id }]
        };
        var projectViewModel = new ProjectViewModel(new Project
        {
            Workflows = [target, caller],
            Journeys = [journey]
        });
        var context = new TestProjectContext(projectViewModel);
        var dialog = new TestDialogService(true);
        using var library = new WorkflowLibraryViewModel(context, dialog);

        await library.DeleteSelectedWorkflowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(projectViewModel.Model.Workflows, Does.Contain(target));
            Assert.That(library.DeletionReferences, Has.Count.EqualTo(2));
            Assert.That(library.LastDeletionBlockMessage, Does.Contain("Regional"));
            Assert.That(library.LastDeletionBlockMessage, Does.Contain("Caller"));
            Assert.That(dialog.LastTitle, Is.EqualTo("Workflow is in use"));
            Assert.That(context.SaveCount, Is.Zero);
        });
    }

    [Test]
    public async Task DeleteSelectedWorkflowCommand_DeletesConfirmedUnreferencedWorkflow()
    {
        var workflow = new Workflow { Name = "Unused", Steps = [] };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [workflow] });
        var context = new TestProjectContext(projectViewModel);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));

        await library.DeleteSelectedWorkflowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(projectViewModel.Model.Workflows, Is.Empty);
            Assert.That(projectViewModel.Workflows, Is.Empty);
            Assert.That(library.SelectedWorkflow, Is.Null);
            Assert.That(context.SaveCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task NestedStepChange_PropagatesToLibraryAutoSave()
    {
        var delay = new WorkflowDelayStep { Name = "Wait", DelayMs = 100 };
        var workflow = new Workflow { EntryStepId = delay.Id, Steps = [delay] };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [workflow] });
        var context = new TestProjectContext(projectViewModel);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));

        ((WorkflowDelayStepViewModel)library.SelectedWorkflow!.Steps.Single()).DelayMs = 200;
        await context.Saved.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(delay.DelayMs, Is.EqualTo(200));
            Assert.That(context.SaveCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProjectRefresh_PreservesSharedWrapperAndSelectionIdentity()
    {
        var first = new Workflow { Name = "First" };
        var second = new Workflow { Name = "Second" };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [first, second] });
        var context = new TestProjectContext(projectViewModel);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));
        var selected = projectViewModel.Workflows[1];
        library.SelectWorkflowCommand.Execute(selected);

        projectViewModel.Refresh();

        Assert.Multiple(() =>
        {
            Assert.That(projectViewModel.Workflows[1], Is.SameAs(selected));
            Assert.That(library.SelectedWorkflow, Is.SameAs(selected));
        });
    }

    [Test]
    public void ValidateCommand_ProjectsNavigationReadyIssuesForSelectedWorkflow()
    {
        var workflow = new Workflow { Name = "Invalid", Steps = [] };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [workflow] });
        using var library = new WorkflowLibraryViewModel(
            new TestProjectContext(projectViewModel),
            new TestDialogService(true));

        library.ValidateCommand.Execute(null);

        Assert.That(library.ValidationIssues, Has.Some.Matches<WorkflowValidationIssue>(
            issue => issue.Code == WorkflowValidationCodes.EmptyWorkflow
                && issue.WorkflowId == workflow.Id));
    }

    [Test]
    public void GraphOrderAndReferences_SurviveSaveAndReopen()
    {
        var first = new WorkflowDelayStep { Name = "First", DelayMs = 20 };
        var second = new WorkflowTerminateStep { Name = "Second" };
        first.NextStepId = second.Id;
        var workflow = new Workflow { EntryStepId = first.Id, Steps = [first, second] };
        var project = new Project { Workflows = [workflow] };

        var json = JsonSerializer.Serialize(project, JsonOptions.Compact);
        var reopened = JsonSerializer.Deserialize<Project>(json, JsonOptions.Compact)!;
        var reopenedViewModel = new ProjectViewModel(reopened).Workflows.Single();

        Assert.Multiple(() =>
        {
            Assert.That(reopenedViewModel.Steps.Select(step => step.Name), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(reopenedViewModel.EntryStepId, Is.EqualTo(reopenedViewModel.Steps[0].Id));
            Assert.That(reopenedViewModel.Steps[0].NextStepId, Is.EqualTo(reopenedViewModel.Steps[1].Id));
        });
    }

    private sealed class TestProjectContext(ProjectViewModel selectedProject) : IProjectContext
    {
        private ProjectViewModel? _selectedProject = selectedProject;
        private JourneyViewModel? _selectedJourney;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ProjectViewModel? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedProject)));
            }
        }

        public JourneyViewModel? SelectedJourney
        {
            get => _selectedJourney;
            set
            {
                _selectedJourney = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedJourney)));
            }
        }

        public SolutionViewModel? SolutionViewModel => null;

        public int SaveCount { get; private set; }

        public TaskCompletionSource Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SaveSolutionInternalAsync()
        {
            SaveCount++;
            Saved.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class TestDialogService(bool result) : IDialogService
    {
        public string LastTitle { get; private set; } = string.Empty;

        public Task<bool> ShowConfirmationAsync(
            string title,
            string message,
            string confirmButtonText = "Yes",
            string cancelButtonText = "No",
            bool isCancelDefault = true)
        {
            LastTitle = title;
            _ = message;
            _ = confirmButtonText;
            _ = cancelButtonText;
            _ = isCancelDefault;
            return Task.FromResult(result);
        }
    }
}
