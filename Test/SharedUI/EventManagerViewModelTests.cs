// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

[TestFixture]
internal sealed class EventManagerViewModelTests
{
    [Test]
    public void AddStep_InsertsAfterSelectionWithDefaultInPortAndSelectsNewStep()
    {
        var viewModel = CreateViewModel();
        var previousFirst = viewModel.Steps[0].Model.Id;
        var previousLast = viewModel.Steps[1].Model.Id;
        viewModel.DefaultInPort = 17;

        viewModel.AddStepCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Steps, Has.Count.EqualTo(3));
            Assert.That(viewModel.Steps[0].Model.Id, Is.EqualTo(previousFirst));
            Assert.That(viewModel.Steps[2].Model.Id, Is.EqualTo(previousLast));
            Assert.That(viewModel.SelectedStep, Is.SameAs(viewModel.Steps[1]));
            Assert.That(viewModel.SelectedStep!.InPort, Is.EqualTo(17));
            Assert.That(viewModel.SelectedStep.RepeatCount, Is.EqualTo(1));
            Assert.That(viewModel.SelectedStep.IsSelected, Is.True);
            Assert.That(viewModel.Steps.Count(step => step.IsSelected), Is.EqualTo(1));
            Assert.That(viewModel.UndoCommand.CanExecute(null), Is.True);
        });
    }

    [Test]
    public void AddStep_WithoutSelectionAppendsToSequence()
    {
        var viewModel = CreateViewModel();
        viewModel.SelectedStep = null;

        viewModel.AddStepCommand.Execute(null);

        Assert.That(viewModel.SelectedStep, Is.SameAs(viewModel.Steps[2]));
    }

    [Test]
    public void AddAndDelete_CanBeUndoneAndRedone()
    {
        var viewModel = CreateViewModel();
        viewModel.AddStepCommand.Execute(null);
        var addedId = viewModel.SelectedStep!.Model.Id;

        viewModel.DeleteStepCommand.Execute(viewModel.SelectedStep);
        Assert.That(viewModel.Steps.Any(step => step.Model.Id == addedId), Is.False);

        viewModel.UndoCommand.Execute(null);
        Assert.That(viewModel.Steps.Any(step => step.Model.Id == addedId), Is.True);
        Assert.That(viewModel.RedoCommand.CanExecute(null), Is.True);

        viewModel.UndoCommand.Execute(null);
        Assert.That(viewModel.Steps, Has.Count.EqualTo(2));
        Assert.That(viewModel.UndoCommand.CanExecute(null), Is.False);

        viewModel.RedoCommand.Execute(null);
        Assert.That(viewModel.Steps.Any(step => step.Model.Id == addedId), Is.True);

        viewModel.RedoCommand.Execute(null);
        Assert.That(viewModel.Steps.Any(step => step.Model.Id == addedId), Is.False);
        Assert.That(viewModel.RedoCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void ToolbarCommands_FollowJourneyAndSelectionAvailability()
    {
        var viewModel = CreateViewModel();
        var journey = viewModel.SelectedJourney;
        var deleteNotifications = 0;
        var addNotifications = 0;
        viewModel.DeleteStepCommand.CanExecuteChanged += (_, _) => deleteNotifications++;
        viewModel.AddStepCommand.CanExecuteChanged += (_, _) => addNotifications++;

        Assert.That(viewModel.AddStepCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.DeleteStepCommand.CanExecute(viewModel.SelectedStep), Is.True);
        viewModel.SelectedStep = null;
        Assert.That(viewModel.DeleteStepCommand.CanExecute(null), Is.False);
        Assert.That(deleteNotifications, Is.GreaterThan(0));

        viewModel.SelectedJourney = null;
        Assert.That(viewModel.AddStepCommand.CanExecute(null), Is.False);
        Assert.That(addNotifications, Is.GreaterThan(0));
        viewModel.AddStepCommand.Execute(null);
        Assert.That(viewModel.Steps, Is.Empty);

        viewModel.SelectedJourney = journey;
        Assert.That(viewModel.AddStepCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.DeleteStepCommand.CanExecute(viewModel.SelectedStep), Is.True);
    }

    [Test]
    public void SelectingAnotherRow_LeavesOnlyThatRowSelectedWithoutChangingSequence()
    {
        var viewModel = CreateViewModel();
        var first = viewModel.Steps[0];
        var second = viewModel.Steps[1];

        Assert.That(first.IsSelected, Is.True);
        viewModel.SelectedStep = second;

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSelected, Is.False);
            Assert.That(second.IsSelected, Is.True);
            Assert.That(viewModel.Steps.Count(step => step.IsSelected), Is.EqualTo(1));
            Assert.That(viewModel.CanUndo, Is.False);
        });
    }

    [Test]
    public void MovingThenUndoing_KeepsSelectionOnAVisibleRow()
    {
        var viewModel = CreateViewModel();
        var moved = viewModel.Steps[0];

        viewModel.MoveStep(moved, 2);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SelectedStep, Is.SameAs(viewModel.Steps[1]));
            Assert.That(viewModel.SelectedStep!.Model.Id, Is.EqualTo(moved.Model.Id));
            Assert.That(viewModel.Steps.Count(step => step.IsSelected), Is.EqualTo(1));
            Assert.That(moved.IsSelected, Is.False);
        });

        viewModel.UndoCommand.Execute(null);

        Assert.That(viewModel.SelectedStep, Is.SameAs(viewModel.Steps[0]));
        Assert.That(viewModel.Steps.Count(step => step.IsSelected), Is.EqualTo(1));
    }

    [Test]
    public void DeletingLastStep_ClearsSelection()
    {
        var viewModel = CreateViewModel();
        viewModel.DeleteStepCommand.Execute(viewModel.SelectedStep);
        var remaining = viewModel.SelectedStep;

        viewModel.DeleteStepCommand.Execute(remaining);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SelectedStep, Is.Null);
            Assert.That(viewModel.Steps, Is.Empty);
            Assert.That(remaining!.IsSelected, Is.False);
        });
    }

    private static EventManagerViewModel CreateViewModel()
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.Setup(value => value.Current).Returns(new MobaRuntimeSnapshot());
        runtime.Setup(value => value.GetTrafficPackets()).Returns([]);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        var main = new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(), runtime.Object, new Mock<IEventBus>().Object,
            dispatcher.Object, new AppSettings(), new Solution(),
            new ActionExecutionContext { Z21 = new Mock<IZ21>().Object },
            new Mock<ILogger<MainWindowViewModel>>().Object);
        var project = new ProjectViewModel(new Project
        {
            Journeys = [new Journey { FeedbackSequence = [new JourneyFeedbackStep(), new JourneyFeedbackStep()] }]
        });
        main.SelectedProject = project;
        main.SelectedJourney = project.Journeys.Single();
        return new EventManagerViewModel(main);
    }
}
