// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.Action;

using Moq;

using System.ComponentModel;
using System.Text.Json;

/// <summary>Verifies shared workflow catalog identity, action operations, references, and save coordination.</summary>
[TestFixture]
public sealed class WorkflowLibraryViewModelTests
{
    [Test]
    public async Task CreateWorkflowCommand_AddsEmptyActionListThroughProjectWrapper()
    {
        var project = new ProjectViewModel(new Project());
        var context = new TestProjectContext(project);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));
        await library.CreateWorkflowCommand.ExecuteAsync(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(project.Model.Workflows, Has.Count.EqualTo(1));
            Assert.That(library.SelectedWorkflow, Is.SameAs(project.Workflows.Single()));
            Assert.That(library.SelectedWorkflow!.Actions, Is.Empty);
            Assert.That(library.SelectedEditorObject, Is.SameAs(library.SelectedWorkflow));
            Assert.That(context.SaveCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task DuplicateSelectedWorkflowCommand_ClonesPayloadAndRemapsIdentifiers()
    {
        var action = Command("Command");
        var source = new Workflow { Name = "Source", Actions = [action, Command("Second")] };
        var project = new ProjectViewModel(new Project { Workflows = [source] });
        var context = new TestProjectContext(project);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));
        await library.DuplicateSelectedWorkflowCommand.ExecuteAsync(null);
        var duplicate = library.SelectedWorkflow!.Model;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(duplicate.Id, Is.Not.EqualTo(source.Id));
            Assert.That(duplicate.Actions.Select(item => item.Id).Intersect(source.Actions.Select(item => item.Id)), Is.Empty);
            Assert.That(duplicate.Actions.Select(item => item.Name), Is.EqualTo(source.Actions.Select(item => item.Name)));
            Assert.That(duplicate.Actions[0].Command, Is.Not.SameAs(action.Command));
            Assert.That(duplicate.Actions[0].Command!.BytesBase64, Is.EqualTo(action.Command!.BytesBase64));
            Assert.That(project.Workflows, Has.Count.EqualTo(2));
            Assert.That(context.SaveCount, Is.EqualTo(1));
        }
        duplicate.Actions[0].Command!.BytesBase64 = "AQID";
        Assert.That(action.Command!.BytesBase64, Is.EqualTo("AA=="));
    }

    [Test]
    public async Task DeleteSelectedWorkflowCommand_BlocksAndReportsMultipleEventAssignments()
    {
        var target = new Workflow { Name = "Target" };
        var journey = new Journey
        {
            Name = "Regional",
            EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { InPort = 1, Count = 3, WorkflowId = target.Id }, new JourneyEvent { InPort = 2, Count = 5, WorkflowId = target.Id }] }
        };
        var project = new ProjectViewModel(new Project { Workflows = [target], Journeys = [journey] });
        var context = new TestProjectContext(project);
        var dialog = new TestDialogService(true);
        using var library = new WorkflowLibraryViewModel(context, dialog);
        await library.DeleteSelectedWorkflowCommand.ExecuteAsync(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(project.Model.Workflows, Does.Contain(target));
            Assert.That(library.DeletionReferences, Has.Count.EqualTo(2));
            Assert.That(library.LastDeletionBlockMessage, Does.Contain("Regional").And.Contain("InPort 1, count 3").And.Contain("InPort 2, count 5"));
            Assert.That(dialog.LastTitle, Is.EqualTo("Workflow is in use"));
            Assert.That(context.SaveCount, Is.Zero);
        }
    }

    [Test]
    public async Task DeleteSelectedWorkflowCommand_DeletesConfirmedUnreferencedWorkflow()
    {
        var workflow = new Workflow { Name = "Unused" };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [workflow] });
        var context = new TestProjectContext(projectViewModel);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));

        await library.DeleteSelectedWorkflowCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(projectViewModel.Model.Workflows, Is.Empty);
            Assert.That(projectViewModel.Workflows, Is.Empty);
            Assert.That(library.SelectedWorkflow, Is.Null);
            Assert.That(context.SaveCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ActionSettingsChange_PropagatesToLibraryAutoSave()
    {
        var delay = Command("Wait");
        delay.DelayAfterMs = 100;
        var workflow = new Workflow { Actions = [delay] };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [workflow] });
        var context = new TestProjectContext(projectViewModel);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));

        library.SelectedWorkflow!.Actions.Single().DelayAfterMs = 200;
        await context.Saved.Task.WaitAsync(TimeSpan.FromSeconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(delay.DelayAfterMs, Is.EqualTo(200));
            Assert.That(context.SaveCount, Is.EqualTo(1));
        }
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(projectViewModel.Workflows[1], Is.SameAs(selected));
            Assert.That(library.SelectedWorkflow, Is.SameAs(selected));
        }
    }

    [Test]
    public void ValidateCommand_ProjectsNavigationReadyIssuesForSelectedWorkflow()
    {
        var workflow = new Workflow { Name = "Invalid" };
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
    public void ReorderingPreservesSelectionAndPersistsExecutionOrder()
    {
        var first = Command("First");
        var second = Command("Second");
        var project = new ProjectViewModel(new Project { Workflows = [new Workflow { Actions = [first, second] }] });
        using var library = new WorkflowLibraryViewModel(new TestProjectContext(project), new TestDialogService(true));
        var selected = library.SelectedAction;
        library.MoveSelectedActionDownCommand.Execute(null);
        var reopened = JsonSerializer.Deserialize<Project>(JsonSerializer.Serialize(project.Model, JsonOptions.Compact), JsonOptions.Compact)!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(reopened.Workflows[0].Actions.Select(action => action.Id), Is.EqualTo(new[] { second.Id, first.Id }));
            Assert.That(reopened.Workflows[0].Actions.Select(action => action.Number), Is.EqualTo(new uint[] { 1, 2 }));
            Assert.That(library.SelectedAction, Is.SameAs(selected));
            Assert.That(library.MoveSelectedActionDownCommand.CanExecute(null), Is.False);
            Assert.That(library.MoveSelectedActionUpCommand.CanExecute(null), Is.True);
        }
    }

    [Test]
    public void ActionEditingDoesNotResetCatalogSelectionAndSelectsNewAction()
    {
        var project = new ProjectViewModel(new Project { Workflows = [new Workflow()] });
        using var library = new WorkflowLibraryViewModel(new TestProjectContext(project), new TestDialogService(true));
        var workflow = library.SelectedWorkflow;
        // A ListView can clear selection when its ItemsSource is replaced.
        library.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(library.FilteredWorkflows)) library.SelectedWorkflow = null;
        };
        library.AddActionCommand.Execute(ActionType.Command);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.SelectedWorkflow, Is.SameAs(workflow));
            Assert.That(library.SelectedAction, Is.SameAs(workflow!.Actions.Single()));
            Assert.That(library.SelectedEditorObject, Is.SameAs(library.SelectedAction));
        }
        library.ShowWorkflowSettingsCommand.Execute(null);
        Assert.That(library.SelectedEditorObject, Is.SameAs(workflow));
        library.SelectActionCommand.Execute(workflow!.Actions[0]);
        library.DeleteSelectedActionCommand.Execute(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflow.Model.Actions, Is.Empty);
            Assert.That(library.SelectedAction, Is.Null);
            Assert.That(library.DeleteSelectedActionCommand.CanExecute(null), Is.False);
        }
    }

    [TestCase(ActionType.Announcement)]
    [TestCase(ActionType.Audio)]
    [TestCase(ActionType.Command)]
    [TestCase(ActionType.SelectSignalAspect)]
    [TestCase(ActionType.ExecuteScript)]
    [TestCase(ActionType.TrainDestinationDisplay)]
    [TestCase(ActionType.ChangeJourneyStop)]
    public void AddActionProvidesTypedEditor(ActionType type)
    {
        var project = new ProjectViewModel(new Project { Workflows = [new Workflow()] });
        using var library = new WorkflowLibraryViewModel(new TestProjectContext(project), new TestDialogService(true));
        library.AddActionCommand.Execute(type);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.SelectedAction, Is.Not.Null);
            Assert.That(library.SelectedAction!.Type, Is.EqualTo(type));
            Assert.That(library.SelectedAction.GetType(), Is.Not.EqualTo(typeof(WorkflowActionViewModel)));
            Assert.That(project.Model.Workflows[0].Actions.Single().Type, Is.EqualTo(type));
        }
    }

    [Test]
    public void ProjectChangeClearsPreviousActionSelection()
    {
        var project = new ProjectViewModel(new Project { Workflows = [new Workflow { Actions = [Command("First")] }] });
        var context = new TestProjectContext(project);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));
        context.SelectedProject = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.SelectedWorkflow, Is.Null);
            Assert.That(library.SelectedAction, Is.Null);
            Assert.That(library.AddActionCommand.CanExecute(ActionType.Command), Is.False);
        }
    }

    [Test]
    public async Task DryRunSelectedWorkflowCommand_PlansWithoutRequestingLiveExecution()
    {
        var workflow = new Workflow { Name = "Preview", Actions = [Command("Preview")] };
        var project = new Project { Workflows = [workflow] };
        var projectViewModel = new ProjectViewModel(project);
        var plannedEffect = new WorkflowPlannedEffect(
            ActionType.Command,
            WorkflowEffectCategory.CommandStation,
            "Send command",
            []);
        WorkflowExecutionRequest? capturedRequest = null;
        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(service => service.ExecuteAsync(
                It.IsAny<WorkflowExecutionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecutionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new WorkflowExecutionResult
            {
                ExecutionId = Guid.NewGuid(),
                WorkflowId = workflow.Id,
                SourceCorrelationId = Guid.NewGuid(),
                Status = WorkflowExecutionStatus.Succeeded,
                PlannedEffects = [plannedEffect]
            });
        var executionContext = new ActionExecutionContext { Z21 = Mock.Of<IZ21>() };
        using var library = new WorkflowLibraryViewModel(
            new TestProjectContext(projectViewModel),
            new TestDialogService(true),
            runtimeServices: new WorkflowLibraryRuntimeServices
            {
                WorkflowService = workflowService.Object,
                ExecutionContext = executionContext
            });

        await library.DryRunSelectedWorkflowCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Mode, Is.EqualTo(WorkflowRunMode.DryRun));
            Assert.That(capturedRequest.Project, Is.SameAs(project));
            Assert.That(capturedRequest.Workflow, Is.SameAs(workflow));
            Assert.That(library.PlannedEffects, Is.EqualTo(new[] { plannedEffect }));
            Assert.That(library.LastDryRunStatus, Is.EqualTo(nameof(WorkflowExecutionStatus.Succeeded)));
            Assert.That(library.IsDryRunRunning, Is.False);
        }
        workflowService.Verify(
            service => service.ExecuteAsync(
                It.IsAny<WorkflowExecutionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void TraceProjection_FiltersSelectedWorkflowAndOrdersNewestFirst()
    {
        var firstWorkflow = new Workflow { Name = "First" };
        var secondWorkflow = new Workflow { Name = "Second" };
        var projectViewModel = new ProjectViewModel(new Project
        {
            Workflows = [firstWorkflow, secondWorkflow]
        });
        var traceStore = new WorkflowTraceStore();
        traceStore.Append(CreateLifecycleEvent(firstWorkflow.Id, 1));
        traceStore.Append(CreateLifecycleEvent(secondWorkflow.Id, 2));
        traceStore.Append(CreateLifecycleEvent(firstWorkflow.Id, 3));
        using var library = new WorkflowLibraryViewModel(
            new TestProjectContext(projectViewModel),
            new TestDialogService(true),
            runtimeServices: new WorkflowLibraryRuntimeServices
            {
                TraceStore = traceStore
            });

        Assert.That(library.TraceEntries.Select(entry => entry.Sequence), Is.EqualTo(new long[] { 3, 1 }));

        library.SelectWorkflowCommand.Execute(projectViewModel.Workflows[1]);

        Assert.That(library.TraceEntries.Select(entry => entry.Sequence), Is.EqualTo(new long[] { 2 }));
    }

    [Test]
    public void NavigateToValidationIssueCommand_SelectsAffectedWorkflowAndAction()
    {
        var firstWorkflow = new Workflow { Name = "First" };
        var affectedStep = Command("Affected");
        var affectedWorkflow = new Workflow
        {
            Name = "Affected workflow",
            Actions = [affectedStep]
        };
        var projectViewModel = new ProjectViewModel(new Project
        {
            Workflows = [firstWorkflow, affectedWorkflow]
        });
        using var library = new WorkflowLibraryViewModel(
            new TestProjectContext(projectViewModel),
            new TestDialogService(true));
        var issue = new WorkflowValidationIssue(
            WorkflowValidationCodes.InvalidActionPayload,
            WorkflowValidationSeverity.Error,
            affectedWorkflow.Id,
            affectedStep.Id,
            "actions[0].delayAfterMs",
            "Delay is invalid.");

        library.NavigateToValidationIssueCommand.Execute(issue);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.SelectedWorkflow!.Model, Is.SameAs(affectedWorkflow));
            Assert.That(library.SelectedAction!.ToWorkflowAction(), Is.SameAs(affectedStep));
        }
    }

    [Test]
    public void ClearingActionsDetachesRemovedActionAndSavesEmptyList()
    {
        var project = new ProjectViewModel(new Project { Workflows = [new Workflow { Actions = [Command("First")] }] });
        var context = new TestProjectContext(project);
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));
        var oldAction = library.SelectedAction!;
        library.SelectedWorkflow!.Actions.Clear();
        var savesAfterClear = context.SaveCount;
        oldAction.Name = "Detached";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(project.Model.Workflows[0].Actions, Is.Empty);
            Assert.That(library.SelectedAction, Is.Null);
            Assert.That(savesAfterClear, Is.GreaterThan(0));
            Assert.That(context.SaveCount, Is.EqualTo(savesAfterClear));
        }
    }

    [Test]
    public void EmptyProjectChangesRefreshGuidanceAndCreationAvailability()
    {
        var context = new TestProjectContext(new ProjectViewModel(new Project()));
        using var library = new WorkflowLibraryViewModel(context, new TestDialogService(true));
        var guidanceChanged = false;
        library.PropertyChanged += (_, e) => guidanceChanged |= e.PropertyName == nameof(library.ActionListHint);
        context.SelectedProject = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(guidanceChanged, Is.True);
            Assert.That(library.CreateWorkflowCommand.CanExecute(null), Is.False);
        }
        context.SelectedProject = new ProjectViewModel(new Project());
        Assert.That(library.CreateWorkflowCommand.CanExecute(null), Is.True);
    }

    private static WorkflowAction Command(string name) => new() { Name = name, Type = ActionType.Command, Command = new() { BytesBase64 = "AA==" } };

    private static WorkflowLifecycleEvent CreateLifecycleEvent(Guid workflowId, long sequence) => new()
    {
        Kind = WorkflowLifecycleKind.WorkflowCompleted,
        SourceCorrelationId = Guid.NewGuid(),
        ExecutionId = Guid.NewGuid(),
        WorkflowId = workflowId,
        Sequence = sequence,
        Mode = WorkflowLifecycleMode.DryRun,
        TimestampUtc = DateTimeOffset.UtcNow
    };

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

        public SolutionSaveState SolutionSaveState => SolutionSaveState.Saved;

        public string SolutionSaveStatusText => "Saved";

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
