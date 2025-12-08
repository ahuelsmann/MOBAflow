// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.SharedUI.ViewModel;

namespace Moba.Test.SharedUI;

public class JourneyViewModelTests
{
    [Test]
    public void JourneyViewModel_ReflectsSessionStateChanges()
    {
        // Arrange
        var station = new Station { Id = Guid.NewGuid(), Name = "Station1", NumberOfLapsToStop = 1 };
        var project = new Project
        {
            Stations = new List<Station> { station }
        };
        
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Name = "TestJourney",
            StationIds = new List<Guid> { station.Id }
        };

        var state = new JourneySessionState
        {
            JourneyId = journey.Id,
            Counter = 5,
            CurrentPos = 0,
            CurrentStationName = "Station1"
        };

        var vm = new JourneyViewModel(journey, project, state);

        // Assert - SessionState properties are exposed via ViewModel
        Assert.That(vm.CurrentCounter, Is.EqualTo(5));
        Assert.That(vm.CurrentPos, Is.EqualTo(0));
        Assert.That(vm.CurrentStation, Is.EqualTo("Station1"));
    }

    [Test]
    public void JourneyViewModel_SettersUpdateModel()
    {
        var project = new Project();
        var journey = new Journey();
        var vm = new JourneyViewModel(journey, project);

        vm.Name = "NewName";
        Assert.That(journey.Name, Is.EqualTo("NewName"));

        vm.InPort = 7;
        Assert.That(journey.InPort, Is.EqualTo(7u));
    }
}
