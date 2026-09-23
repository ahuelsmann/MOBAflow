// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.ViewModel;

[TestFixture]
internal sealed class JourneyFeedbackStepViewModelTests
{
    [Test]
    public void AssignAndRemoveWorkflow_ChangesOnlyAssignment()
    {
        var workflowA = new Workflow { Name = "A" };
        var workflowB = new Workflow { Name = "B" };
        var project = new Project { Workflows = [workflowA, workflowB] };
        var step = new JourneyFeedbackStep();
        var viewModel = new JourneyFeedbackStepViewModel(step, project);

        viewModel.AssignWorkflowCommand.Execute(new WorkflowViewModel(workflowA));
        viewModel.AssignWorkflowCommand.Execute(new WorkflowViewModel(workflowB));
        Assert.That(step.WorkflowId, Is.EqualTo(workflowB.Id));

        viewModel.RemoveWorkflowCommand.Execute(null);
        Assert.That(step.WorkflowId, Is.Null);
        Assert.That(step.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void AssignAndRemoveStation_UpdatesDirectTransition()
    {
        var station = new Station { Name = "Herford Hauptbahnhof" };
        var journey = new Journey { Stations = [station] };
        var step = new JourneyFeedbackStep();
        var viewModel = new JourneyFeedbackStepViewModel(step, new Project(), journey);

        viewModel.AssignStationCommand.Execute(new StationAssignmentOption(station.Name, JourneyStopTransitionMode.SpecificStation, station));
        Assert.That(step.StopTransition.Mode, Is.EqualTo(JourneyStopTransitionMode.SpecificStation));
        Assert.That(step.StopTransition.StationId, Is.EqualTo(station.Id));

        viewModel.RemoveStopTransitionCommand.Execute(null);
        Assert.That(step.StopTransition.Mode, Is.EqualTo(JourneyStopTransitionMode.None));
        Assert.That(step.StopTransition.StationId, Is.Null);
    }

    [Test]
    public void RepeatCount_IsClampedToOne()
    {
        var step = new JourneyFeedbackStep();
        var viewModel = new JourneyFeedbackStepViewModel(step, new Project());

        viewModel.RepeatCount = 0;

        Assert.That(step.Index, Is.EqualTo(1));
        Assert.That(viewModel.IsRepeat, Is.False);
    }

    [Test]
    public void EditingRepeatCount_RefreshesVisibleRuntimeProgress()
    {
        var viewModel = new JourneyFeedbackStepViewModel(new JourneyFeedbackStep { Index = 10 }, new Project());
        viewModel.UpdateRuntimeProgress(true, 3);
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.RepeatCount = 12;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.RepeatCountText, Is.EqualTo("12x"));
            Assert.That(viewModel.CompactRuntimeProgress, Is.EqualTo("3/12"));
            Assert.That(viewModel.RuntimeProgress, Is.EqualTo("Current progress: 3/12"));
            Assert.That(notifications, Does.Contain(nameof(viewModel.RepeatCountText)));
            Assert.That(notifications, Does.Contain(nameof(viewModel.CompactRuntimeProgress)));
            Assert.That(notifications, Does.Contain(nameof(viewModel.RuntimeProgress)));
        });
    }

    [Test]
    public void RuntimeMovesToAnotherStep_ClearsProgressFromPreviousRow()
    {
        var viewModel = new JourneyFeedbackStepViewModel(new JourneyFeedbackStep { Index = 10 }, new Project());
        viewModel.UpdateRuntimeProgress(true, 3);

        viewModel.UpdateRuntimeProgress(false, 0);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.CompactRuntimeProgress, Is.Empty);
            Assert.That(viewModel.RuntimeProgress, Is.Empty);
            Assert.That(viewModel.AutomationName, Does.Not.Contain("Current progress"));
        });
    }

    [Test]
    public void AssignmentAndEnabledChanges_RefreshAccessibleRowDescription()
    {
        var workflow = new Workflow { Name = "Arrival" };
        var viewModel = new JourneyFeedbackStepViewModel(new JourneyFeedbackStep(), new Project { Workflows = [workflow] });
        var accessibleNameChanges = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.AutomationName)) accessibleNameChanges++;
        };

        viewModel.WorkflowId = workflow.Id;
        viewModel.Enabled = false;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.AutomationName, Does.Contain("Arrival").And.Contain("disabled"));
            Assert.That(accessibleNameChanges, Is.EqualTo(2));
        });
    }
}
