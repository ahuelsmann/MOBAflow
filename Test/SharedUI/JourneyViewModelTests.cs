// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.SharedUI.ViewModel;

namespace Moba.Test.SharedUI;

public class JourneyViewModelTests
{
    [Test]
    public void JourneyViewModel_ReflectsModelChanges()
    {
        // Arrange
        var journey = new Journey();
        journey.Name = "TestJourney";
        journey.Stations = new List<Station>();
        var station = new Station { Name = "Station1", NumberOfLapsToStop = 1 };
        journey.Stations.Add(station);

        var vm = new JourneyViewModel(journey);

        // Act
        journey.CurrentCounter = 5;
        journey.CurrentPos = 1;

        // Assert
        Assert.That(vm.CurrentCounter, Is.EqualTo(5u));
        Assert.That(vm.CurrentPos, Is.EqualTo(1u));
    }

    [Test]
    public void JourneyViewModel_SettersUpdateModel()
    {
        var journey = new Journey();
        var vm = new JourneyViewModel(journey);

        vm.Name = "NewName";
        Assert.That(journey.Name, Is.EqualTo("NewName"));

        vm.InPort = 7;
        Assert.That(journey.InPort, Is.EqualTo(7u));
    }
}
