// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using NUnit.Framework;

/// <summary>
/// Unit tests for Editor ViewModels CRUD operations.
/// Tests JourneyEditor, WorkflowEditor, TrainEditor, LocomotiveEditor, and WagonEditor.
/// </summary>
[TestFixture]
public class EditorViewModelTests
{
    private Project _project = null!;
    private ValidationService _validationService = null!;

    [SetUp]
    public void Setup()
    {
        _project = new Project { Name = "Test Project" };
        _validationService = new ValidationService(_project);
    }

    #region JourneyEditorViewModel Tests

    [Test]
    public void JourneyEditor_AddJourney_AddsToProjectAndCollection()
    {
        // Arrange
        var editor = new JourneyEditorViewModel(_project, _validationService);
        var initialCount = editor.Journeys.Count;

        // Act
        editor.AddJourneyCommand.Execute(null);

        // Assert
        Assert.That(editor.Journeys.Count, Is.EqualTo(initialCount + 1));
        Assert.That(_project.Journeys.Count, Is.EqualTo(initialCount + 1));
        Assert.That(editor.SelectedJourney, Is.Not.Null);
        Assert.That(editor.SelectedJourney?.Name, Is.EqualTo("New Journey"));
    }

    [Test]
    public void JourneyEditor_DeleteJourney_RemovesFromProjectAndCollection()
    {
        // Arrange
        var editor = new JourneyEditorViewModel(_project, _validationService);
        editor.AddJourneyCommand.Execute(null);
        var initialCount = editor.Journeys.Count;

        // Act
        editor.DeleteJourneyCommand.Execute(null);

        // Assert
        Assert.That(editor.Journeys.Count, Is.EqualTo(initialCount - 1));
        Assert.That(_project.Journeys.Count, Is.EqualTo(initialCount - 1));
        Assert.That(editor.SelectedJourney, Is.Null);
    }

    [Test]
    public void JourneyEditor_DeleteJourney_WithReference_SetsValidationError()
    {
        // Arrange
        var editor = new JourneyEditorViewModel(_project, _validationService);
        editor.AddJourneyCommand.Execute(null);
        var journey1 = editor.SelectedJourney!;
        
        // Create a second journey that references the first
        var journey2 = new Journey { Name = "Journey2", NextJourney = journey1.Name };
        _project.Journeys.Add(journey2);
        editor.Journeys.Add(journey2);
        
        editor.SelectedJourney = journey1;

        // Act
        editor.DeleteJourneyCommand.Execute(null);

        // Assert
        Assert.That(editor.ValidationError, Is.Not.Null);
        Assert.That(editor.ValidationError, Does.Contain("Journey2"));
        Assert.That(_project.Journeys, Does.Contain(journey1)); // Not deleted
    }

    [Test]
    public void JourneyEditor_AddStation_AddsToSelectedJourney()
    {
        // Arrange
        var editor = new JourneyEditorViewModel(_project, _validationService);
        editor.AddJourneyCommand.Execute(null);
        var initialStationCount = editor.Stations.Count;

        // Act
        editor.AddStationCommand.Execute(null);

        // Assert
        Assert.That(editor.Stations.Count, Is.EqualTo(initialStationCount + 1));
        Assert.That(editor.SelectedJourney!.Stations.Count, Is.EqualTo(initialStationCount + 1));
    }

    #endregion

    #region WorkflowEditorViewModel Tests

    [Test]
    public void WorkflowEditor_AddWorkflow_AddsToProjectAndCollection()
    {
        // Arrange
        var editor = new WorkflowEditorViewModel(_project, _validationService);
        var initialCount = editor.Workflows.Count;

        // Act
        editor.AddWorkflowCommand.Execute(null);

        // Assert
        Assert.That(editor.Workflows.Count, Is.EqualTo(initialCount + 1));
        Assert.That(_project.Workflows.Count, Is.EqualTo(initialCount + 1));
        Assert.That(editor.SelectedWorkflow, Is.Not.Null);
        Assert.That(editor.SelectedWorkflow?.Name, Is.EqualTo("New Workflow"));
    }

    [Test]
    public void WorkflowEditor_DeleteWorkflow_RemovesFromProjectAndCollection()
    {
        // Arrange
        var editor = new WorkflowEditorViewModel(_project, _validationService);
        editor.AddWorkflowCommand.Execute(null);
        var initialCount = editor.Workflows.Count;

        // Act
        editor.DeleteWorkflowCommand.Execute(null);

        // Assert
        Assert.That(editor.Workflows.Count, Is.EqualTo(initialCount - 1));
        Assert.That(_project.Workflows.Count, Is.EqualTo(initialCount - 1));
        Assert.That(editor.SelectedWorkflow, Is.Null);
    }

    [Test]
    public void WorkflowEditor_DeleteWorkflow_WithReference_SetsValidationError()
    {
        // Arrange
        var editor = new WorkflowEditorViewModel(_project, _validationService);
        editor.AddWorkflowCommand.Execute(null);
        var workflow = editor.SelectedWorkflow!;
        
        // Create a Journey with a Station that references the workflow
        var station = new Station { Name = "Station1", Flow = workflow };
        var journey = new Journey { Name = "Journey1" };
        journey.Stations.Add(station);
        _project.Journeys.Add(journey);

        // Act
        editor.DeleteWorkflowCommand.Execute(null);

        // Assert
        Assert.That(editor.ValidationError, Is.Not.Null);
        Assert.That(editor.ValidationError, Does.Contain("Station1"));
        Assert.That(_project.Workflows, Does.Contain(workflow)); // Not deleted
    }

    [Test]
    public void WorkflowEditor_AddAnnouncement_AddsToSelectedWorkflow()
    {
        // Arrange
        var editor = new WorkflowEditorViewModel(_project, _validationService);
        editor.AddWorkflowCommand.Execute(null);
        var initialActionCount = editor.Actions.Count;

        // Act
        editor.AddAnnouncementCommand.Execute(null);

        // Assert
        Assert.That(editor.Actions.Count, Is.EqualTo(initialActionCount + 1));
        Assert.That(editor.SelectedWorkflow!.Actions.Count, Is.EqualTo(initialActionCount + 1));
    }

    #endregion

    #region TrainEditorViewModel Tests

    [Test]
    public void TrainEditor_AddTrain_AddsToProjectAndCollection()
    {
        // Arrange
        var editor = new TrainEditorViewModel(_project, _validationService);
        var initialCount = editor.Trains.Count;

        // Act
        editor.AddTrainCommand.Execute(null);

        // Assert
        Assert.That(editor.Trains.Count, Is.EqualTo(initialCount + 1));
        Assert.That(_project.Trains.Count, Is.EqualTo(initialCount + 1));
        Assert.That(editor.SelectedTrain, Is.Not.Null);
        Assert.That(editor.SelectedTrain?.Name, Is.EqualTo("New Train"));
    }

    [Test]
    public void TrainEditor_DeleteTrain_RemovesFromProjectAndCollection()
    {
        // Arrange
        var editor = new TrainEditorViewModel(_project, _validationService);
        editor.AddTrainCommand.Execute(null);
        var initialCount = editor.Trains.Count;

        // Act
        editor.DeleteTrainCommand.Execute(null);

        // Assert
        Assert.That(editor.Trains.Count, Is.EqualTo(initialCount - 1));
        Assert.That(_project.Trains.Count, Is.EqualTo(initialCount - 1));
        Assert.That(editor.SelectedTrain, Is.Null);
    }

    [Test]
    public void TrainEditor_DeleteTrain_WithReference_SetsValidationError()
    {
        // Arrange
        var editor = new TrainEditorViewModel(_project, _validationService);
        editor.AddTrainCommand.Execute(null);
        var train = editor.SelectedTrain!;
        
        // Create a Journey that references the train
        var journey = new Journey { Name = "Journey1", Train = train };
        _project.Journeys.Add(journey);

        // Act
        editor.DeleteTrainCommand.Execute(null);

        // Assert
        Assert.That(editor.ValidationError, Is.Not.Null);
        Assert.That(editor.ValidationError, Does.Contain("Journey1"));
        Assert.That(_project.Trains, Does.Contain(train)); // Not deleted
    }

    [Test]
    public void TrainEditor_AddLocomotive_AddsToSelectedTrain()
    {
        // Arrange
        var editor = new TrainEditorViewModel(_project, _validationService);
        editor.AddTrainCommand.Execute(null);
        var initialLocomotiveCount = editor.Locomotives.Count;

        // Act
        editor.AddLocomotiveToCompositionCommand.Execute(null);

        // Assert
        Assert.That(editor.Locomotives.Count, Is.EqualTo(initialLocomotiveCount + 1));
        Assert.That(editor.SelectedTrain!.Locomotives.Count, Is.EqualTo(initialLocomotiveCount + 1));
    }

    [Test]
    public void TrainEditor_AddWagon_AddsToSelectedTrain()
    {
        // Arrange
        var editor = new TrainEditorViewModel(_project, _validationService);
        editor.AddTrainCommand.Execute(null);
        var initialWagonCount = editor.Wagons.Count;

        // Act
        editor.AddWagonToCompositionCommand.Execute(null);

        // Assert
        Assert.That(editor.Wagons.Count, Is.EqualTo(initialWagonCount + 1));
        Assert.That(editor.SelectedTrain!.Wagons.Count, Is.EqualTo(initialWagonCount + 1));
    }

    #endregion

    #region LocomotiveEditorViewModel Tests

    [Test]
    public void LocomotiveEditor_AddLocomotive_AddsToProjectAndCollection()
    {
        // Arrange
        var editor = new LocomotiveEditorViewModel(_project, _validationService);
        var initialCount = editor.Locomotives.Count;

        // Act
        editor.AddLocomotiveCommand.Execute(null);

        // Assert
        Assert.That(editor.Locomotives.Count, Is.EqualTo(initialCount + 1));
        Assert.That(_project.Locomotives.Count, Is.EqualTo(initialCount + 1));
        Assert.That(editor.SelectedLocomotive, Is.Not.Null);
    }

    [Test]
    public void LocomotiveEditor_DeleteLocomotive_RemovesFromProjectAndCollection()
    {
        // Arrange
        var editor = new LocomotiveEditorViewModel(_project, _validationService);
        editor.AddLocomotiveCommand.Execute(null);
        var initialCount = editor.Locomotives.Count;

        // Act
        editor.DeleteLocomotiveCommand.Execute(null);

        // Assert
        Assert.That(editor.Locomotives.Count, Is.EqualTo(initialCount - 1));
        Assert.That(_project.Locomotives.Count, Is.EqualTo(initialCount - 1));
    }

    [Test]
    public void LocomotiveEditor_DeleteLocomotive_WithReference_SetsValidationError()
    {
        // Arrange
        var editor = new LocomotiveEditorViewModel(_project, _validationService);
        editor.AddLocomotiveCommand.Execute(null);
        var locomotive = editor.SelectedLocomotive!;
        
        // Create a Train that references the locomotive
        var train = new Train { Name = "Train1" };
        train.Locomotives.Add(locomotive);
        _project.Trains.Add(train);

        // Act
        editor.DeleteLocomotiveCommand.Execute(null);

        // Assert
        Assert.That(editor.ValidationError, Is.Not.Null);
        Assert.That(editor.ValidationError, Does.Contain("Train1"));
        Assert.That(_project.Locomotives, Does.Contain(locomotive)); // Not deleted
    }

    #endregion

    #region WagonEditorViewModel Tests

    [Test]
    public void WagonEditor_AddPassengerWagon_AddsToProjectAndCollection()
    {
        // Arrange
        var editor = new WagonEditorViewModel(_project, _validationService);
        var initialCount = editor.Wagons.Count;

        // Act
        editor.AddPassengerWagonCommand.Execute(null);

        // Assert
        Assert.That(editor.Wagons.Count, Is.EqualTo(initialCount + 1));
        Assert.That(_project.PassengerWagons.Count, Is.EqualTo(initialCount + 1));
        Assert.That(editor.SelectedWagon, Is.Not.Null);
        Assert.That(editor.SelectedWagon, Is.TypeOf<PassengerWagon>());
    }

    [Test]
    public void WagonEditor_AddGoodsWagon_AddsToProjectAndCollection()
    {
        // Arrange
        var editor = new WagonEditorViewModel(_project, _validationService);
        var initialCount = editor.Wagons.Count;

        // Act
        editor.AddGoodsWagonCommand.Execute(null);

        // Assert
        Assert.That(editor.Wagons.Count, Is.EqualTo(initialCount + 1));
        Assert.That(_project.GoodsWagons.Count, Is.EqualTo(initialCount + 1));
        Assert.That(editor.SelectedWagon, Is.Not.Null);
        Assert.That(editor.SelectedWagon, Is.TypeOf<GoodsWagon>());
    }

    [Test]
    public void WagonEditor_DeleteWagon_RemovesFromProjectAndCollection()
    {
        // Arrange
        var editor = new WagonEditorViewModel(_project, _validationService);
        editor.AddPassengerWagonCommand.Execute(null);
        var initialCount = editor.Wagons.Count;

        // Act
        editor.DeleteWagonCommand.Execute(null);

        // Assert
        Assert.That(editor.Wagons.Count, Is.EqualTo(initialCount - 1));
        Assert.That(_project.PassengerWagons.Count, Is.EqualTo(initialCount - 1));
    }

    [Test]
    public void WagonEditor_DeleteWagon_WithReference_SetsValidationError()
    {
        // Arrange
        var editor = new WagonEditorViewModel(_project, _validationService);
        editor.AddPassengerWagonCommand.Execute(null);
        var wagon = editor.SelectedWagon!;
        
        // Create a Train that references the wagon
        var train = new Train { Name = "Train1" };
        train.Wagons.Add(wagon);
        _project.Trains.Add(train);

        // Act
        editor.DeleteWagonCommand.Execute(null);

        // Assert
        Assert.That(editor.ValidationError, Is.Not.Null);
        Assert.That(editor.ValidationError, Does.Contain("Train1"));
        Assert.That(editor.Wagons, Does.Contain(wagon)); // Not deleted
    }

    #endregion
}
