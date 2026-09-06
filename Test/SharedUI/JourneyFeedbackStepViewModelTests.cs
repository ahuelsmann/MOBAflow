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

        Assert.That(step.RepeatCount, Is.EqualTo(1));
        Assert.That(viewModel.IsRepeat, Is.False);
    }
}
