// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Test.SharedUI;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Tests for StationViewModel - ViewModel wrapper for Station domain model.
/// </summary>
[TestFixture]
public class StationViewModelTests
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
}
