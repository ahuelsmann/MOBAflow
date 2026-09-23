// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.WorkflowSteps;

using Moq;

using System.ComponentModel;
using System.Text.Json;

/// <summary>Verifies shared workflow catalog identity, graph operations, references, and save coordination.</summary>
[TestFixture]
public sealed class WorkflowLibraryViewModelTests
{
    [TestCase(WorkflowStepKind.Action)]
    [TestCase(WorkflowStepKind.Delay)]
    [TestCase(WorkflowStepKind.Condition)]
    [TestCase(WorkflowStepKind.Parallel)]
    [TestCase(WorkflowStepKind.NestedWorkflow)]
    [TestCase(WorkflowStepKind.Terminate)]
    public void AddStep_PreservesSelectionWhenBoundCatalogRefreshWouldClearIt(WorkflowStepKind kind)
    {
        var end = new WorkflowTerminateStep { Name = "Done" };
        var project = new ProjectViewModel(new Project
        {
            Workflows = [new Workflow { EntryStepId = end.Id, Steps = [end] }]
        });
        var context = new TestProjectContext(project);
        using var library = new WorkflowLibraryViewModel(context);
        var workflow = library.SelectedWorkflow!;
        // Model a TwoWay ListView selection reset when its filtered ItemsSource is replaced.
        library.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowLibraryViewModel.FilteredWorkflows)) library.SelectedWorkflow = null;
        };

        Assert.DoesNotThrow(() => library.AddStepCommand.Execute(kind));

        Assert.Multiple(() =>
        {
            Assert.That(library.SelectedWorkflow, Is.SameAs(workflow));
            Assert.That(library.SelectedStep, Is.SameAs(workflow.Steps.Last()));
            Assert.That(workflow.Steps, Has.Count.EqualTo(2));
            Assert.That(workflow.Steps.Last().Kind, Is.EqualTo(kind));
            Assert.That(context.SaveCount, Is.EqualTo(1));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AddStep_DoesNotOverwriteSelectionChangedBySynchronousNotification(bool selectOtherWorkflow)
    {
        var end = new WorkflowTerminateStep { Name = "Done" };
        var otherEnd = new WorkflowTerminateStep { Name = "Other end" };
        var project = new ProjectViewModel(new Project
        {
            Workflows =
            [
                new Workflow { EntryStepId = end.Id, Steps = [end] },
                new Workflow { EntryStepId = otherEnd.Id, Steps = [otherEnd] }
            ]
        });
        using var library = new WorkflowLibraryViewModel(new TestProjectContext(project));
        var workflow = library.SelectedWorkflow!;
        var otherWorkflow = selectOtherWorkflow ? project.Workflows[1] : null;
        workflow.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(WorkflowViewModel.Steps)) return;
            library.SelectedWorkflow = otherWorkflow;
            library.SelectedStep = null;
        };

        Assert.DoesNotThrow(() => library.AddStepCommand.Execute(WorkflowStepKind.Delay));

        Assert.Multiple(() =>
        {
            Assert.That(library.SelectedWorkflow, Is.SameAs(otherWorkflow));
            Assert.That(library.SelectedStep, Is.Null);
            Assert.That(workflow.Steps, Has.Count.EqualTo(2));
            Assert.That(project.Workflows[1].Steps, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void NamedWorkflowSelection_UsesSharedWrappersAndRefreshesRenamedTarget()
    {
        var nested = new WorkflowNestedStep { Name = "Call" };
        var caller = new Workflow { Name = "Caller", EntryStepId = nested.Id, Steps = [nested] };
        var target = new Workflow { Name = "Arrival", Steps = [] };
        var project = new ProjectViewModel(new Project { Workflows = [caller, target] });
        var context = new TestProjectContext(project);
        using var library = new WorkflowLibraryViewModel(context);
        var editor = (WorkflowNestedStepViewModel)library.SelectedStep!;

        editor.InvokedWorkflow = project.Workflows[1];
        project.Workflows[1].Name = "Arrival announcement";

        Assert.Multiple(() =>
        {
            Assert.That(nested.WorkflowId, Is.EqualTo(target.Id));
            Assert.That(editor.InvokedWorkflow, Is.SameAs(project.Workflows[1]));
            Assert.That(editor.ConnectionsSummary, Does.Contain("Call: Arrival announcement"));
            Assert.That(context.SaveCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void RenameWorkflow_StillRefreshesSearchResults()
    {
        var project = new ProjectViewModel(new Project { Workflows = [new Workflow { Name = "Old name" }] });
        using var library = new WorkflowLibraryViewModel(new TestProjectContext(project));
        library.SearchText = "Arrival";
        Assert.That(library.FilteredWorkflows, Is.Empty);
        var refreshes = 0;
        library.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowLibraryViewModel.FilteredWorkflows)) refreshes++;
        };

        project.Workflows.Single().Name = "Arrival announcement";

        Assert.Multiple(() =>
        {
            Assert.That(library.FilteredWorkflows.Single(), Is.SameAs(project.Workflows.Single()));
            Assert.That(refreshes, Is.EqualTo(1));
        });
    }

    [Test]
    public void NamedConditionContext_OffersJourneyAndStopChoicesForSelectedKind()
    {
        var station = new Station { Name = "Central" };
        var journey = new Journey { Name = "Regional", Stations = [station] };
        var condition = new WorkflowConditionStep { Condition = new CurrentJourneyWorkflowCondition() };
        var workflow = new Workflow { EntryStepId = condition.Id, Steps = [condition] };
        var project = new ProjectViewModel(new Project { Workflows = [workflow], Journeys = [journey] });
        using var library = new WorkflowLibraryViewModel(new TestProjectContext(project));
        var editor = (WorkflowConditionStepViewModel)library.SelectedStep!;

        editor.ContextEntity = editor.ContextChoices.Single();
        Assert.That(((CurrentJourneyWorkflowCondition)condition.Condition).JourneyId, Is.EqualTo(journey.Id));

        editor.ConditionKind = WorkflowConditionKind.CurrentStation;
        editor.ContextEntity = editor.ContextChoices.Single();

        Assert.Multiple(() =>
        {
            Assert.That(editor.ContextEntity!.Name, Is.EqualTo("Central"));
            Assert.That(((CurrentStationWorkflowCondition)condition.Condition).StationId, Is.EqualTo(station.Id));
            Assert.That(editor.IsFeedbackCondition, Is.False);
            Assert.That(editor.IsContextCondition, Is.True);
        });
    }

    [Test]
    public void AddStep_SelectsNewEditor_AndWorkflowSettingsDoNotSaveOrChangeConnections()
    {
        var end = new WorkflowTerminateStep { Name = "Done" };
        var workflow = new Workflow { EntryStepId = end.Id, Steps = [end] };
        var context = new TestProjectContext(new ProjectViewModel(new Project { Workflows = [workflow] }));
        using var library = new WorkflowLibraryViewModel(context);

        library.AddStepCommand.Execute(WorkflowStepKind.Delay);
        Assert.That(library.SelectedStep, Is.SameAs(library.SelectedWorkflow!.Steps.Last()));
        library.EditWorkflowSettingsCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(library.SelectedEditorObject, Is.SameAs(library.SelectedWorkflow));
            Assert.That(context.SaveCount, Is.EqualTo(1));
            Assert.That(workflow.EntryStepId, Is.EqualTo(end.Id));
        });
    }

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

    [Test]
    public async Task DryRunSelectedWorkflowCommand_PlansWithoutRequestingLiveExecution()
    {
        var terminate = new WorkflowTerminateStep { Name = "Done" };
        var workflow = new Workflow { Name = "Preview", EntryStepId = terminate.Id, Steps = [terminate] };
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

        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Mode, Is.EqualTo(WorkflowRunMode.DryRun));
            Assert.That(capturedRequest.Project, Is.SameAs(project));
            Assert.That(capturedRequest.Workflow, Is.SameAs(workflow));
            Assert.That(library.PlannedEffects, Is.EqualTo(new[] { plannedEffect }));
            Assert.That(library.LastDryRunStatus, Is.EqualTo(nameof(WorkflowExecutionStatus.Succeeded)));
            Assert.That(library.IsDryRunRunning, Is.False);
        });
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
    public void NavigateToValidationIssueCommand_SelectsAffectedWorkflowAndStep()
    {
        var firstWorkflow = new Workflow { Name = "First" };
        var affectedStep = new WorkflowDelayStep { Name = "Affected" };
        var affectedWorkflow = new Workflow
        {
            Name = "Affected workflow",
            EntryStepId = affectedStep.Id,
            Steps = [affectedStep]
        };
        var projectViewModel = new ProjectViewModel(new Project
        {
            Workflows = [firstWorkflow, affectedWorkflow]
        });
        using var library = new WorkflowLibraryViewModel(
            new TestProjectContext(projectViewModel),
            new TestDialogService(true));
        var issue = new WorkflowValidationIssue(
            WorkflowValidationCodes.InvalidStepPayload,
            WorkflowValidationSeverity.Error,
            affectedWorkflow.Id,
            affectedStep.Id,
            "steps[0].delayMs",
            "Delay is invalid.");

        library.NavigateToValidationIssueCommand.Execute(issue);

        Assert.Multiple(() =>
        {
            Assert.That(library.SelectedWorkflow!.Model, Is.SameAs(affectedWorkflow));
            Assert.That(library.SelectedStep!.Model, Is.SameAs(affectedStep));
        });
    }

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
