// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Tests for StationViewModel - ViewModel wrapper for Station domain model.
/// </summary>
[TestFixture]
internal class StationViewModelTests
{
    private Station _station = null!;
    private Project _project = null!;
    private StationViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
        _station = new Station
        {
            Id = Guid.NewGuid(),
            Name = "Berlin Hbf",
            Description = "Hauptbahnhof Berlin",
            InPort = 15,
            NumberOfLapsToStop = 2
        };
        _project = new Project();
        _viewModel = new StationViewModel(_station, _project);
    }

    [Test]
    public void Constructor_InitializesFromModel()
    {
        Assert.That(_viewModel.Name, Is.EqualTo("Berlin Hbf"));
        Assert.That(_viewModel.Description, Is.EqualTo("Hauptbahnhof Berlin"));
        Assert.That(_viewModel.InPort, Is.EqualTo(15));
        Assert.That(_viewModel.NumberOfLapsToStop, Is.EqualTo(2));
    }

    [Test]
    public void Model_ReturnsUnderlyingStation()
    {
        Assert.That(_viewModel.Model, Is.SameAs(_station));
    }

    [Test]
    public void Name_SetValue_UpdatesModel()
    {
        _viewModel.Name = "München Hbf";

        Assert.That(_station.Name, Is.EqualTo("München Hbf"));
    }

    [Test]
    public void Description_SetValue_UpdatesModel()
    {
        _viewModel.Description = "Hauptbahnhof München";

        Assert.That(_station.Description, Is.EqualTo("Hauptbahnhof München"));
    }

    [Test]
    public void InPort_SetValue_UpdatesModel()
    {
        _viewModel.InPort = 42;

        Assert.That(_station.InPort, Is.EqualTo(42u));
    }

    [Test]
    public void NumberOfLapsToStop_SetValue_UpdatesModel()
    {
        _viewModel.NumberOfLapsToStop = 5;

        Assert.That(_station.NumberOfLapsToStop, Is.EqualTo(5u));
    }

    [Test]
    public void WorkflowId_InitiallyNull()
    {
        Assert.That(_viewModel.WorkflowId, Is.Null);
    }

    [Test]
    public void WorkflowId_SetValue_UpdatesModel()
    {
        var workflowId = Guid.NewGuid();
        _viewModel.WorkflowId = workflowId;

        Assert.That(_station.WorkflowId, Is.EqualTo(workflowId));
    }

    [Test]
    public void RemoveWorkflowCommand_WithAssignedWorkflow_ClearsWorkflow()
    {
        var workflowId = Guid.NewGuid();
        _viewModel.WorkflowId = workflowId;

        _viewModel.RemoveWorkflowCommand.Execute(null);

        Assert.That(_station.WorkflowId, Is.Null);
        Assert.That(_viewModel.WorkflowId, Is.Null);
        Assert.That(_viewModel.HasWorkflow, Is.False);
    }

    [Test]
    public void PlatformRemoveWorkflowCommand_WithAssignedWorkflow_ClearsWorkflow()
    {
        var platform = new Platform { WorkflowId = Guid.NewGuid() };
        var viewModel = new PlatformViewModel(platform, _project);

        viewModel.RemoveWorkflowCommand.Execute(null);

        Assert.That(platform.WorkflowId, Is.Null);
        Assert.That(viewModel.WorkflowId, Is.Null);
        Assert.That(viewModel.HasWorkflow, Is.False);
    }

    [Test]
    public void IsVirtual_SetValue_UpdatesModelAndComputedProperties()
    {
        _viewModel.IsVirtual = true;

        Assert.That(_station.IsVirtual, Is.True);
        Assert.That(_viewModel.IsRealStation, Is.False);
        Assert.That(_viewModel.StationKindText, Is.EqualTo("Event"));
        Assert.That(_viewModel.StationIconGlyph, Is.EqualTo("\uE945"));
        Assert.That(_viewModel.StationForegroundResourceKey, Is.EqualTo("TextFillColorPrimaryBrush"));
    }

    [Test]
    public void StationIconGlyph_ForRealStation_ReturnsCityGlyph()
    {
        Assert.That(_viewModel.StationIconGlyph, Is.EqualTo("\uEC06"));
        Assert.That(_viewModel.StationKindText, Is.EqualTo("Station"));
        Assert.That(_viewModel.StationForegroundResourceKey, Is.EqualTo("TextFillColorPrimaryBrush"));
    }

    [Test]
    public void Name_SetValue_RaisesPropertyChanged()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StationViewModel.Name))
                propertyChangedRaised = true;
        };

        _viewModel.Name = "New Station Name";

        Assert.That(propertyChangedRaised, Is.True);
    }

    [Test]
    public void ArrivalTimeText_WithArrival_ReturnsHourMinute()
    {
        _viewModel.Arrival = new DateTime(2026, 5, 6, 8, 15, 0);

        Assert.That(_viewModel.ArrivalTimeText, Is.EqualTo("08:15"));
    }

    [Test]
    public void ArrivalTimeText_WithoutArrival_ReturnsPlaceholder()
    {
        _viewModel.Arrival = null;

        Assert.That(_viewModel.ArrivalTimeText, Is.EqualTo("--:--"));
    }

    [Test]
    public void DepartureTimeText_WithDeparture_ReturnsHourMinute()
    {
        _viewModel.Departure = new DateTime(2026, 5, 6, 18, 45, 0);

        Assert.That(_viewModel.DepartureTimeText, Is.EqualTo("18:45"));
    }

    [Test]
    public void DepartureTimeText_WithoutDeparture_ReturnsPlaceholder()
    {
        _viewModel.Departure = null;

        Assert.That(_viewModel.DepartureTimeText, Is.EqualTo("--:--"));
    }
}
