// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.SharedUI;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Tests for WorkflowViewModel - ViewModel wrapper for Workflow domain model.
/// </summary>
[TestFixture]
public class WorkflowViewModelTests
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
            Parameters = new Dictionary<string, object> { ["FilePath"] = "gong.wav" }
        });

        var vm = new WorkflowViewModel(workflow);

        Assert.That(vm.Actions, Has.Count.EqualTo(1));
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
}
