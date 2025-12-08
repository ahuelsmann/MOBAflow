// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.Service;
using NUnit.Framework;

/// <summary>
/// Unit tests for ValidationService.
/// Tests all validation rules for delete operations.
/// </summary>
[TestFixture]
public class ValidationServiceTests
{
    private Project _project = null!;
    private ValidationService _validationService = null!;

    [SetUp]
    public void Setup()
    {
        _project = new Project { Name = "Test Project" };
        _validationService = new ValidationService(_project);
    }

    #region Journey Validation Tests

    [Test]
    public void CanDeleteJourney_WhenNotReferenced_ReturnsSuccess()
    {
        // Arrange
        var journey = new Journey { Name = "Journey1" };
        _project.Journeys.Add(journey);

        // Act
        var result = _validationService.CanDeleteJourney(journey);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void CanDeleteJourney_WhenReferencedByNextJourney_ReturnsFailure()
    {
        // Arrange
        var journey1 = new Journey { Id = Guid.NewGuid(), Name = "Journey1" };
        var journey2 = new Journey { Id = Guid.NewGuid(), Name = "Journey2", NextJourneyId = journey1.Id };
        _project.Journeys.Add(journey1);
        _project.Journeys.Add(journey2);

        // Act
        var result = _validationService.CanDeleteJourney(journey1);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Journey2"));
        Assert.That(result.ErrorMessage, Does.Contain("NextJourney"));
    }

    [Test]
    public void CanDeleteJourney_WhenNull_ReturnsFailure()
    {
        // Act
        var result = _validationService.CanDeleteJourney(null!);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    #endregion

    #region Workflow Validation Tests

    [Test]
    public void CanDeleteWorkflow_WhenNotReferenced_ReturnsSuccess()
    {
        // Arrange
        var workflow = new Workflow { Name = "Workflow1" };
        _project.Workflows.Add(workflow);

        // Act
        var result = _validationService.CanDeleteWorkflow(workflow);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void CanDeleteWorkflow_WhenReferencedByStation_ReturnsFailure()
    {
        // Arrange
        var workflow = new Workflow { Id = Guid.NewGuid(), Name = "Workflow1" };
        var station = new Station { Id = Guid.NewGuid(), Name = "Station1", WorkflowId = workflow.Id };
        var journey = new Journey { Id = Guid.NewGuid(), Name = "Journey1", StationIds = new List<Guid> { station.Id } };
        
        _project.Workflows.Add(workflow);
        _project.Stations.Add(station);
        _project.Journeys.Add(journey);

        // Act
        var result = _validationService.CanDeleteWorkflow(workflow);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Station1"));
        Assert.That(result.ErrorMessage, Does.Contain("Journey1"));
    }

    [Test]
    public void CanDeleteWorkflow_WhenReferencedByPlatform_ReturnsFailure()
    {
        // Arrange
        var workflow = new Workflow { Name = "Workflow1" };
        var platform = new Platform { Name = "Platform1", WorkflowId = workflow.Id };
        var station = new Station { Id = Guid.NewGuid(), Name = "Station1", Platforms = new List<Platform> { platform } };
        var journey = new Journey { Id = Guid.NewGuid(), Name = "Journey1", StationIds = new List<Guid> { station.Id } };
        
        _project.Workflows.Add(workflow);
        _project.Stations.Add(station);
        _project.Journeys.Add(journey);

        // Act
        var result = _validationService.CanDeleteWorkflow(workflow);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Platform1"));
        Assert.That(result.ErrorMessage, Does.Contain("Station1"));
    }

    [Test]
    public void CanDeleteWorkflow_WhenNull_ReturnsFailure()
    {
        // Act
        var result = _validationService.CanDeleteWorkflow(null!);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    #endregion

    #region Train Validation Tests

    [Test]
    public void CanDeleteTrain_WhenNotReferenced_ReturnsSuccess()
    {
        // Arrange
        var train = new Train { Name = "Train1" };
        _project.Trains.Add(train);

        // Act
        var result = _validationService.CanDeleteTrain(train);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void CanDeleteTrain_WhenReferencedByJourney_ReturnsFailure()
    {
        // Arrange
        var train = new Train { Name = "Train1" };
        var journey = new Journey { Name = "Journey1" };
        
        _project.Trains.Add(train);
        _project.Journeys.Add(journey);

        // Act
        var result = _validationService.CanDeleteTrain(train);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Train1"));
        Assert.That(result.ErrorMessage, Does.Contain("Journey1"));
    }

    [Test]
    public void CanDeleteTrain_WhenNull_ReturnsFailure()
    {
        // Act
        var result = _validationService.CanDeleteTrain(null!);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    #endregion

    #region Locomotive Validation Tests

    [Test]
    public void CanDeleteLocomotive_WhenNotReferenced_ReturnsSuccess()
    {
        // Arrange
        var locomotive = new Locomotive { Name = "Loco1" };
        _project.Locomotives.Add(locomotive);

        // Act
        var result = _validationService.CanDeleteLocomotive(locomotive);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void CanDeleteLocomotive_WhenReferencedByTrain_ReturnsFailure()
    {
        // Arrange
        var locomotive = new Locomotive { Id = Guid.NewGuid(), Name = "Loco1" };
        var train = new Train { Id = Guid.NewGuid(), Name = "Train1", LocomotiveIds = new List<Guid> { locomotive.Id } };
        
        _project.Locomotives.Add(locomotive);
        _project.Trains.Add(train);

        // Act
        var result = _validationService.CanDeleteLocomotive(locomotive);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Loco1"));
        Assert.That(result.ErrorMessage, Does.Contain("Train1"));
    }

    [Test]
    public void CanDeleteLocomotive_WhenNull_ReturnsFailure()
    {
        // Act
        var result = _validationService.CanDeleteLocomotive(null!);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    #endregion

    #region Wagon Validation Tests

    [Test]
    public void CanDeleteWagon_WhenNotReferenced_ReturnsSuccess()
    {
        // Arrange
        var wagon = new PassengerWagon { Name = "Wagon1" };
        _project.PassengerWagons.Add(wagon);

        // Act
        var result = _validationService.CanDeleteWagon(wagon);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void CanDeleteWagon_WhenReferencedByTrain_ReturnsFailure()
    {
        // Arrange
        var wagon = new PassengerWagon { Id = Guid.NewGuid(), Name = "Wagon1" };
        var train = new Train { Id = Guid.NewGuid(), Name = "Train1", WagonIds = new List<Guid> { wagon.Id } };
        
        _project.PassengerWagons.Add(wagon);
        _project.Trains.Add(train);

        // Act
        var result = _validationService.CanDeleteWagon(wagon);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Wagon1"));
        Assert.That(result.ErrorMessage, Does.Contain("Train1"));
    }

    [Test]
    public void CanDeleteWagon_WhenNull_ReturnsFailure()
    {
        // Act
        var result = _validationService.CanDeleteWagon(null!);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    #endregion

    #region Station Validation Tests

    [Test]
    public void CanDeleteStation_Always_ReturnsSuccess()
    {
        // Arrange
        var station = new Station { Name = "Station1" };

        // Act
        var result = _validationService.CanDeleteStation(station);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void CanDeleteStation_WhenNull_ReturnsFailure()
    {
        // Act
        var result = _validationService.CanDeleteStation(null!);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    #endregion

    #region Complex Scenarios

    [Test]
    public void CanDeleteWorkflow_WhenReferencedByMultipleStations_ReturnsFailure()
    {
        // Arrange
        var workflow = new Workflow { Id = Guid.NewGuid(), Name = "Workflow1" };
        var station1 = new Station { Id = Guid.NewGuid(), Name = "Station1", WorkflowId = workflow.Id };
        var station2 = new Station { Id = Guid.NewGuid(), Name = "Station2", WorkflowId = workflow.Id };
        var journey = new Journey { Id = Guid.NewGuid(), Name = "Journey1", StationIds = new List<Guid> { station1.Id, station2.Id } };
        
        _project.Workflows.Add(workflow);
        _project.Stations.Add(station1);
        _project.Stations.Add(station2);
        _project.Journeys.Add(journey);

        // Act
        var result = _validationService.CanDeleteWorkflow(workflow);

        // Assert
        Assert.That(result.IsValid, Is.False);
        // Should report first reference found
        Assert.That(result.ErrorMessage, Does.Contain("Station1"));
    }

    [Test]
    public void CanDeleteLocomotive_WhenReferencedByMultipleTrains_ReturnsFailure()
    {
        // Arrange
        var locomotive = new Locomotive { Id = Guid.NewGuid(), Name = "Loco1" };
        var train1 = new Train { Id = Guid.NewGuid(), Name = "Train1", LocomotiveIds = new List<Guid> { locomotive.Id } };
        var train2 = new Train { Id = Guid.NewGuid(), Name = "Train2", LocomotiveIds = new List<Guid> { locomotive.Id } };
        
        _project.Locomotives.Add(locomotive);
        _project.Trains.Add(train1);
        _project.Trains.Add(train2);

        // Act
        var result = _validationService.CanDeleteLocomotive(locomotive);

        // Assert
        Assert.That(result.IsValid, Is.False);
        // Should report first reference found
        Assert.That(result.ErrorMessage, Does.Contain("Train1"));
    }

    #endregion
}
