// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Service;
using Moba.SharedUI.ViewModel;

[TestFixture]
internal class WinuiJourneyViewModelTests
{
    [Test]
    public void SessionState_CanBeModified()
    {
        var journey = new Journey { Id = Guid.NewGuid() };
        var state = new JourneySessionState { JourneyId = journey.Id };

        // Act
        state.Counter++;
        state.CurrentPos++;

        // Assert
        Assert.That(state.Counter, Is.EqualTo(1));
        Assert.That(state.CurrentPos, Is.EqualTo(1));
    }

    [Test]
    public void MoveStationTo_ReordersStationsInViewModelAndModel()
    {
        var first = new Station { Name = "Event1", IsVirtual = true };
        var second = new Station { Name = "Bielefeld" };
        var third = new Station { Name = "Event2", IsVirtual = true };
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Stations = [first, second, third]
        };
        var viewModel = new JourneyViewModel(journey, new Project());

        viewModel.MoveStationTo(viewModel.Stations[0], 3);

        Assert.That(viewModel.Stations.Select(station => station.Name), Is.EqualTo(new[] { "Bielefeld", "Event2", "Event1" }));
        Assert.That(journey.Stations.Select(station => station.Name), Is.EqualTo(new[] { "Bielefeld", "Event2", "Event1" }));
        Assert.That(viewModel.Stations.Select(station => station.Position), Is.EqualTo(new[] { 1, 2, 3 }));
    }
}