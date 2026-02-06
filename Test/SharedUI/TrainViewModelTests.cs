// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain.Enum;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Tests for TrainViewModel - ViewModel wrapper for Train domain model.
/// </summary>
[TestFixture]
public class TrainViewModelTests
{
    private Train _train = null!;
    private Project _project = null!;
    private TrainViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
        _train = new Train
        {
            Id = Guid.NewGuid(),
            Name = "ICE 123",
            Description = "München - Hamburg",
            IsDoubleTraction = true,
            TrainType = TrainType.Passenger,
            ServiceType = ServiceType.InterCityExpress
        };
        _project = new Project();
        _viewModel = new TrainViewModel(_train, _project);
    }

    [Test]
    public void Constructor_InitializesFromModel()
    {
        Assert.That(_viewModel.Id, Is.EqualTo(_train.Id));
        Assert.That(_viewModel.Name, Is.EqualTo("ICE 123"));
        Assert.That(_viewModel.Description, Is.EqualTo("München - Hamburg"));
        Assert.That(_viewModel.IsDoubleTraction, Is.True);
        Assert.That(_viewModel.TrainType, Is.EqualTo(TrainType.Passenger));
        Assert.That(_viewModel.ServiceType, Is.EqualTo(ServiceType.InterCityExpress));
    }

    [Test]
    public void Model_ReturnsUnderlyingTrain()
    {
        Assert.That(_viewModel.Model, Is.SameAs(_train));
    }

    [Test]
    public void Name_SetValue_UpdatesModel()
    {
        _viewModel.Name = "RE 456";

        Assert.That(_train.Name, Is.EqualTo("RE 456"));
    }

    [Test]
    public void Description_SetValue_UpdatesModel()
    {
        _viewModel.Description = "Berlin - Dresden";

        Assert.That(_train.Description, Is.EqualTo("Berlin - Dresden"));
    }

    [Test]
    public void IsDoubleTraction_SetValue_UpdatesModel()
    {
        _viewModel.IsDoubleTraction = false;

        Assert.That(_train.IsDoubleTraction, Is.False);
    }

    [Test]
    public void TrainType_SetValue_UpdatesModel()
    {
        _viewModel.TrainType = TrainType.Freight;

        Assert.That(_train.TrainType, Is.EqualTo(TrainType.Freight));
    }

    [Test]
    public void ServiceType_SetValue_UpdatesModel()
    {
        _viewModel.ServiceType = ServiceType.RegionalExpress;

        Assert.That(_train.ServiceType, Is.EqualTo(ServiceType.RegionalExpress));
    }

    [Test]
    public void Name_SetValue_RaisesPropertyChanged()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TrainViewModel.Name))
                propertyChangedRaised = true;
        };

        _viewModel.Name = "New Train Name";

        Assert.That(propertyChangedRaised, Is.True);
    }
}
