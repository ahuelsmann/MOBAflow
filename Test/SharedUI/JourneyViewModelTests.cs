// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.SharedUI.ViewModel;

namespace Moba.Test.SharedUI;

public class JourneyViewModelTests
{
    [Test]
    public void JourneyViewModel_ReflectsSessionStateChanges()
    {
        // Arrange
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Name = "TestJourney",
            Stations = new List<Station>
            {
                new Station { Name = "Station1", NumberOfLapsToStop = 1 }
            }
        };

        var state = new JourneySessionState
        {
            JourneyId = journey.Id,
            Counter = 5,
            CurrentPos = 1,
            CurrentStationName = "Station1"
        };

        var vm = new JourneyViewModel(journey, state);

        // Assert - SessionState properties are exposed via ViewModel
        Assert.That(vm.CurrentCounter, Is.EqualTo(5));
        Assert.That(vm.CurrentPos, Is.EqualTo(1));
        Assert.That(vm.CurrentStation, Is.EqualTo("Station1"));
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
