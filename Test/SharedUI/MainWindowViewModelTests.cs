// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.ViewModel;
using Moba.Test.TestBase;
using ActionVM = Moba.SharedUI.ViewModel.Action; // Alias to avoid conflict with System.Action

namespace Moba.Test.SharedUI;

[TestFixture]
public class MainWindowViewModelTests : ViewModelTestBase
{
    private MainWindowViewModel CreateViewModel()
    {
        var solution = new Solution();
        solution.Projects.Add(new Project { Name = "Test Project" });
        
        var settings = new Common.Configuration.AppSettings();
        
        return new MainWindowViewModel(
            IoServiceMock.Object,
            Z21Mock.Object,
            JourneyManagerFactoryMock.Object,
            UiDispatcherMock.Object,
            settings,
            solution);
    }

    #region Journey CRUD Tests

    [Test]
    public void AddJourney_CreatesNewJourneyInProject()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialCount = vm.CurrentProjectViewModel?.Journeys.Count ?? 0;

        // Act
        vm.AddJourneyCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Journeys.Count, Is.EqualTo(initialCount + 1));
        Assert.That(vm.SelectedJourney, Is.Not.Null);
        Assert.That(vm.SelectedJourney?.Name, Is.EqualTo("New Journey"));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void DeleteJourney_RemovesSelectedJourney()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddJourneyCommand.Execute(null);
        var journeyToDelete = vm.SelectedJourney;
        var initialCount = vm.CurrentProjectViewModel?.Journeys.Count ?? 0;

        // Act
        vm.DeleteJourneyCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Journeys.Count, Is.EqualTo(initialCount - 1));
        Assert.That(vm.SelectedJourney, Is.Null);
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void DeleteJourney_CannotExecuteWhenNoSelection()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedJourney = null;

        // Act & Assert
        Assert.That(vm.DeleteJourneyCommand.CanExecute(null), Is.False);
    }

    #endregion

    #region Station CRUD Tests

    [Test]
    public void AddStation_CreatesNewStationInJourney()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddJourneyCommand.Execute(null);
        var journey = vm.SelectedJourney;
        var initialCount = journey?.Stations.Count ?? 0;

        // Act
        vm.AddStationCommand.Execute(null);

        // Assert
        Assert.That(journey?.Stations.Count, Is.EqualTo(initialCount + 1));
        Assert.That(vm.SelectedStation, Is.Not.Null);
        Assert.That(vm.SelectedStation?.Name, Is.EqualTo("New Station"));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void DeleteStation_RemovesSelectedStation()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddJourneyCommand.Execute(null);
        vm.AddStationCommand.Execute(null);
        var initialCount = vm.SelectedJourney?.Stations.Count ?? 0;

        // Act
        vm.DeleteStationCommand.Execute(null);

        // Assert
        Assert.That(vm.SelectedJourney?.Stations.Count, Is.EqualTo(initialCount - 1));
        Assert.That(vm.SelectedStation, Is.Null);
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void AddStation_CannotExecuteWithoutJourney()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedJourney = null;

        // Act & Assert
        Assert.That(vm.AddStationCommand.CanExecute(null), Is.False);
    }

    #endregion

    #region Workflow CRUD Tests

    [Test]
    public void AddWorkflow_CreatesNewWorkflowInProject()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialCount = vm.CurrentProjectViewModel?.Workflows.Count ?? 0;

        // Act
        vm.AddWorkflowCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Workflows.Count, Is.EqualTo(initialCount + 1));
        Assert.That(vm.SelectedWorkflow, Is.Not.Null);
        Assert.That(vm.SelectedWorkflow?.Name, Is.EqualTo("New Workflow"));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void DeleteWorkflow_RemovesSelectedWorkflow()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddWorkflowCommand.Execute(null);
        var initialCount = vm.CurrentProjectViewModel?.Workflows.Count ?? 0;

        // Act
        vm.DeleteWorkflowCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Workflows.Count, Is.EqualTo(initialCount - 1));
        Assert.That(vm.SelectedWorkflow, Is.Null);
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    #endregion

    #region Train CRUD Tests

    [Test]
    public void AddTrain_CreatesNewTrainInProject()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialCount = vm.CurrentProjectViewModel?.Trains.Count ?? 0;

        // Act
        vm.AddTrainCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Trains.Count, Is.EqualTo(initialCount + 1));
        Assert.That(vm.SelectedTrain, Is.Not.Null);
        Assert.That(vm.SelectedTrain?.Name, Is.EqualTo("New Train"));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void DeleteTrain_RemovesSelectedTrain()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddTrainCommand.Execute(null);
        var initialCount = vm.CurrentProjectViewModel?.Trains.Count ?? 0;

        // Act
        vm.DeleteTrainCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Trains.Count, Is.EqualTo(initialCount - 1));
        Assert.That(vm.SelectedTrain, Is.Null);
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    #endregion

    #region Locomotive CRUD Tests

    [Test]
    public void AddLocomotive_CreatesNewLocomotiveInProject()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialCount = vm.CurrentProjectViewModel?.Locomotives.Count ?? 0;

        // Act
        vm.AddLocomotiveCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Locomotives.Count, Is.EqualTo(initialCount + 1));
        Assert.That(vm.SelectedLocomotive, Is.Not.Null);
        Assert.That(vm.SelectedLocomotive?.Name, Is.EqualTo("New Locomotive"));
        Assert.That(vm.SelectedLocomotive?.DigitalAddress, Is.EqualTo(3));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void DeleteLocomotive_RemovesSelectedLocomotive()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddLocomotiveCommand.Execute(null);
        var initialCount = vm.CurrentProjectViewModel?.Locomotives.Count ?? 0;

        // Act
        vm.DeleteLocomotiveCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Locomotives.Count, Is.EqualTo(initialCount - 1));
        Assert.That(vm.SelectedLocomotive, Is.Null);
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    #endregion

    #region Wagon CRUD Tests

    [Test]
    public void AddPassengerWagon_CreatesNewPassengerWagonInProject()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialWagonCount = vm.CurrentProjectViewModel?.Wagons.Count ?? 0;
        var initialPassengerCount = vm.CurrentProjectViewModel?.Model.PassengerWagons.Count ?? 0;

        // Act
        vm.AddPassengerWagonCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Wagons.Count, Is.EqualTo(initialWagonCount + 1));
        Assert.That(vm.CurrentProjectViewModel?.Model.PassengerWagons.Count, Is.EqualTo(initialPassengerCount + 1));
        Assert.That(vm.SelectedWagon, Is.Not.Null);
        Assert.That(vm.SelectedWagon, Is.InstanceOf<PassengerWagonViewModel>());
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void AddGoodsWagon_CreatesNewGoodsWagonInProject()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialWagonCount = vm.CurrentProjectViewModel?.Wagons.Count ?? 0;
        var initialGoodsCount = vm.CurrentProjectViewModel?.Model.GoodsWagons.Count ?? 0;

        // Act
        vm.AddGoodsWagonCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Wagons.Count, Is.EqualTo(initialWagonCount + 1));
        Assert.That(vm.CurrentProjectViewModel?.Model.GoodsWagons.Count, Is.EqualTo(initialGoodsCount + 1));
        Assert.That(vm.SelectedWagon, Is.Not.Null);
        Assert.That(vm.SelectedWagon, Is.InstanceOf<GoodsWagonViewModel>());
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void DeleteWagon_RemovesSelectedPassengerWagon()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddPassengerWagonCommand.Execute(null);
        var initialCount = vm.CurrentProjectViewModel?.Wagons.Count ?? 0;

        // Act
        vm.DeleteWagonCommand.Execute(null);

        // Assert
        Assert.That(vm.CurrentProjectViewModel?.Wagons.Count, Is.EqualTo(initialCount - 1));
        Assert.That(vm.SelectedWagon, Is.Null);
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    #endregion

    #region Workflow Action Tests

    [Test]
    public void AddAnnouncement_CreatesAnnouncementInWorkflow()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddWorkflowCommand.Execute(null);
        var workflow = vm.SelectedWorkflow;
        var initialCount = workflow?.Actions.Count ?? 0;

        // Act
        vm.AddAnnouncementCommand.Execute(null);

        // Assert
        Assert.That(workflow?.Actions.Count, Is.EqualTo(initialCount + 1));
        Assert.That(workflow?.Actions.Last(), Is.InstanceOf<ActionVM.AnnouncementViewModel>());
        Assert.That(workflow?.Model.Actions.Last().Type, Is.EqualTo(ActionType.Announcement));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void AddCommand_CreatesCommandInWorkflow()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddWorkflowCommand.Execute(null);
        var workflow = vm.SelectedWorkflow;
        var initialCount = workflow?.Actions.Count ?? 0;

        // Act
        vm.AddCommandCommand.Execute(null);

        // Assert
        Assert.That(workflow?.Actions.Count, Is.EqualTo(initialCount + 1));
        Assert.That(workflow?.Actions.Last(), Is.InstanceOf<ActionVM.CommandViewModel>());
        Assert.That(workflow?.Model.Actions.Last().Type, Is.EqualTo(ActionType.Command));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void AddAudio_CreatesAudioInWorkflow()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.AddWorkflowCommand.Execute(null);
        var workflow = vm.SelectedWorkflow;
        var initialCount = workflow?.Actions.Count ?? 0;

        // Act
        vm.AddAudioCommand.Execute(null);

        // Assert
        Assert.That(workflow?.Actions.Count, Is.EqualTo(initialCount + 1));
        Assert.That(workflow?.Actions.Last(), Is.InstanceOf<ActionVM.AudioViewModel>());
        Assert.That(workflow?.Model.Actions.Last().Type, Is.EqualTo(ActionType.Audio));
        Assert.That(vm.HasUnsavedChanges, Is.True);
    }

    #endregion
}

