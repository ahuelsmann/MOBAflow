// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.Action;
using Moba.SharedUI.ViewModel.WorkflowSteps;

/// <summary>
/// Tests for WorkflowViewModel - ViewModel wrapper for Workflow domain model.
/// </summary>
[TestFixture]
internal class WorkflowViewModelTests
{
    private Workflow _workflow = null!;
    private WorkflowViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
        _workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Test Workflow",
            Description = "Test Description",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            InPort = 5,
            IsUsingTimerToIgnoreFeedbacks = true,
            IntervalForTimerToIgnoreFeedbacks = 1500.0
        };
        _viewModel = new WorkflowViewModel(_workflow);
    }

    [Test]
    public void Constructor_InitializesFromModel()
    {
        Assert.That(_viewModel.Id, Is.EqualTo(_workflow.Id));
        Assert.That(_viewModel.Name, Is.EqualTo("Test Workflow"));
        Assert.That(_viewModel.Description, Is.EqualTo("Test Description"));
        Assert.That(_viewModel.ExecutionMode, Is.EqualTo(WorkflowExecutionMode.Sequential));
        Assert.That(_viewModel.InPort, Is.EqualTo(5u));
        Assert.That(_viewModel.IsUsingTimerToIgnoreFeedbacks, Is.True);
        Assert.That(_viewModel.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(1500.0));
    }

    [Test]
    public void Model_ReturnsUnderlyingWorkflow()
    {
        Assert.That(_viewModel.Model, Is.SameAs(_workflow));
    }

    [Test]
    public void Name_SetValue_UpdatesModel()
    {
        _viewModel.Name = "Updated Name";

        Assert.That(_workflow.Name, Is.EqualTo("Updated Name"));
    }

    [Test]
    public void Description_SetValue_UpdatesModel()
    {
        _viewModel.Description = "Updated Description";

        Assert.That(_workflow.Description, Is.EqualTo("Updated Description"));
    }

    [Test]
    public void ExecutionMode_SetValue_UpdatesModel()
    {
        _viewModel.ExecutionMode = WorkflowExecutionMode.Parallel;

        Assert.That(_workflow.ExecutionMode, Is.EqualTo(WorkflowExecutionMode.Parallel));
    }

    [Test]
    public void InPort_SetValue_UpdatesModel()
    {
        _viewModel.InPort = 42;

        Assert.That(_workflow.InPort, Is.EqualTo(42u));
    }

    [Test]
    public void IsUsingTimerToIgnoreFeedbacks_SetValue_UpdatesModel()
    {
        _viewModel.IsUsingTimerToIgnoreFeedbacks = false;

        Assert.That(_workflow.IsUsingTimerToIgnoreFeedbacks, Is.False);
    }

    [Test]
    public void IntervalForTimerToIgnoreFeedbacks_SetValue_UpdatesModel()
    {
        _viewModel.IntervalForTimerToIgnoreFeedbacks = 2500.0;

        Assert.That(_workflow.IntervalForTimerToIgnoreFeedbacks, Is.EqualTo(2500.0));
    }

    [Test]
    public void ExecutionModeValues_ContainsAllEnumValues()
    {
        var values = _viewModel.ExecutionModeValues.ToList();

        Assert.That(values, Does.Contain(WorkflowExecutionMode.Sequential));
        Assert.That(values, Does.Contain(WorkflowExecutionMode.Parallel));
    }

    [Test]
    public void Actions_InitiallyEmpty()
    {
        Assert.That(_viewModel.Actions, Is.Not.Null);
        Assert.That(_viewModel.Actions, Is.Empty);
    }

    [Test]
    public void Actions_WithExistingActions_CreatesViewModels()
    {
        var workflow = new Workflow();
        workflow.Actions.Add(new WorkflowAction
        {
            Name = "Gong",
            Type = ActionType.Audio,
            Number = 1,
            Audio = new AudioActionPayload { FilePath = "gong.wav" }
        });

        var vm = new WorkflowViewModel(workflow);

        Assert.That(vm.Actions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Actions_WithExecuteScriptAction_CreatesPowerShellViewModel()
    {
        var workflow = new Workflow();
        workflow.Actions.Add(new WorkflowAction
        {
            Name = "Run script",
            Type = ActionType.ExecuteScript,
            Number = 1,
            PowerShell = new PowerShellActionPayload { ScriptPath = "script.ps1" }
        });

        var vm = new WorkflowViewModel(workflow);

        Assert.That(vm.Actions.Single(), Is.TypeOf<PowerShellActionViewModel>());
    }

    [Test]
    public void AddActionCommand_WithTrainDestinationDisplay_CreatesDisplayViewModel()
    {
        var workflow = new Workflow();
        var vm = new WorkflowViewModel(workflow);

        vm.AddActionCommand.Execute(ActionType.TrainDestinationDisplay);

        Assert.That(workflow.Actions.Single().Type, Is.EqualTo(ActionType.TrainDestinationDisplay));
        Assert.That(vm.Actions.Single(), Is.TypeOf<TrainDestinationDisplayViewModel>());
    }

    [Test]
    public void Name_SetValue_RaisesPropertyChanged()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.Name))
                propertyChangedRaised = true;
        };

        _viewModel.Name = "New Name";

        Assert.That(propertyChangedRaised, Is.True);
    }

    [Test]
    public void ChildActionPropertyChanged_UpdatesModelAndRaisesActionsChanged()
    {
        // Arrange
        var action = new WorkflowAction
        {
            Name = "Original action",
            Number = 1,
            Type = ActionType.Command,
            Command = new CommandActionPayload()
        };
        var workflow = new Workflow { Actions = [action] };
        var viewModel = new WorkflowViewModel(workflow);
        var actionsChanged = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.Actions))
                actionsChanged++;
        };
        var actionViewModel = (WorkflowActionViewModel)viewModel.Actions.Single();

        // Act
        actionViewModel.Name = "Updated action";

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(action.Name, Is.EqualTo("Updated action"));
            Assert.That(actionsChanged, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProjectViewModel_WorkflowsExposeStableWrappersForProjectModels()
    {
        // Arrange
        var workflow = new Workflow { Name = "Shared workflow" };
        var project = new Project { Workflows = [workflow] };

        // Act
        var projectViewModel = new ProjectViewModel(project);
        var firstAccess = projectViewModel.Workflows.Single();
        var secondAccess = projectViewModel.Workflows.Single();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(firstAccess, Is.SameAs(secondAccess));
            Assert.That(firstAccess.Model, Is.SameAs(workflow));
        });
    }

    [Test]
    public void Constructor_CreatesTypedStepWrappersInPersistedOrder()
    {
        var delay = new WorkflowDelayStep { Name = "Wait", DelayMs = 250 };
        var terminate = new WorkflowTerminateStep { Name = "Done" };
        var workflow = new Workflow { EntryStepId = delay.Id, Steps = [delay, terminate] };

        var viewModel = new WorkflowViewModel(workflow);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Steps, Has.Count.EqualTo(2));
            Assert.That(viewModel.Steps[0], Is.TypeOf<WorkflowDelayStepViewModel>());
            Assert.That(viewModel.Steps[1], Is.TypeOf<WorkflowTerminateStepViewModel>());
            Assert.That(viewModel.Steps.Select(step => step.Model), Is.EqualTo(workflow.Steps));
        });
    }

    [Test]
    public void NestedStepPropertyChanged_PropagatesAsStepsChange()
    {
        var delay = new WorkflowDelayStep { DelayMs = 250 };
        var viewModel = new WorkflowViewModel(new Workflow { EntryStepId = delay.Id, Steps = [delay] });
        var changes = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.Steps)) changes++;
        };

        ((WorkflowDelayStepViewModel)viewModel.Steps.Single()).DelayMs = 500;

        Assert.Multiple(() =>
        {
            Assert.That(delay.DelayMs, Is.EqualTo(500));
            Assert.That(changes, Is.EqualTo(1));
        });
    }

    [Test]
    public void AddStepCommand_AppendsTypedNodeAndConnectsLinearTail()
    {
        var first = new WorkflowDelayStep { Name = "First" };
        var workflow = new Workflow { EntryStepId = first.Id, Steps = [first] };
        var viewModel = new WorkflowViewModel(workflow);

        viewModel.AddStepCommand.Execute(WorkflowStepKind.Terminate);

        Assert.Multiple(() =>
        {
            Assert.That(workflow.Steps, Has.Count.EqualTo(2));
            Assert.That(workflow.Steps![1], Is.TypeOf<WorkflowTerminateStep>());
            Assert.That(first.NextStepId, Is.EqualTo(workflow.Steps[1].Id));
            Assert.That(viewModel.Steps[1].Model, Is.SameAs(workflow.Steps[1]));
        });
    }

    [Test]
    public void MoveStep_PreservesExactModelAndWrapperOrder()
    {
        var first = new WorkflowDelayStep { Name = "First" };
        var second = new WorkflowDelayStep { Name = "Second" };
        var third = new WorkflowTerminateStep { Name = "Third" };
        var workflow = new Workflow { EntryStepId = first.Id, Steps = [first, second, third] };
        var viewModel = new WorkflowViewModel(workflow);

        viewModel.MoveStep(viewModel.Steps[2], 0);

        Assert.Multiple(() =>
        {
            Assert.That(workflow.Steps, Is.EqualTo(new WorkflowStep[] { third, first, second }));
            Assert.That(viewModel.Steps.Select(step => step.Model), Is.EqualTo(workflow.Steps));
        });
    }

    [Test]
    public void ProjectViewModel_RefreshPreservesAuthoritativeWorkflowWrapper()
    {
        var workflow = new Workflow { Name = "Shared workflow" };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [workflow] });
        var original = projectViewModel.Workflows.Single();

        projectViewModel.Refresh();

        Assert.That(projectViewModel.Workflows.Single(), Is.SameAs(original));
    }
}
