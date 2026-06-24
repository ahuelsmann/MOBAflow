// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Service;
using Moba.Domain.Enum;
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

    [Test]
    public void FilteredStations_StopsOnlyMode_ReturnsOnlyRealStations()
    {
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Stations =
            [
                new Station { Name = "Event1", IsVirtual = true },
                new Station { Name = "Bielefeld" },
                new Station { Name = "Event2", IsVirtual = true },
                new Station { Name = "Herford" }
            ]
        };
        var viewModel = new JourneyViewModel(journey, new Project())
        {
            StationListViewMode = StationListViewMode.StopsOnly
        };

        Assert.That(viewModel.FilteredStations.Select(station => station.Name), Is.EqualTo(new[] { "Bielefeld", "Herford" }));
    }

    [Test]
    public void FilteredStations_FullTimelineMode_ReturnsAllStations()
    {
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Stations =
            [
                new Station { Name = "Event1", IsVirtual = true },
                new Station { Name = "Bielefeld" }
            ]
        };
        var viewModel = new JourneyViewModel(journey, new Project())
        {
            StationListViewMode = StationListViewMode.FullTimeline
        };

        Assert.That(viewModel.FilteredStations.Select(station => station.Name), Is.EqualTo(new[] { "Event1", "Bielefeld" }));
    }

    [Test]
    public void UpdateFromSessionState_StopsOnlyMode_HighlightsApproachSegmentForRealStation()
    {
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Stations =
            [
                new Station { Name = "Event1", IsVirtual = true },
                new Station { Name = "Bielefeld" },
                new Station { Name = "Event2", IsVirtual = true },
                new Station { Name = "Herford" }
            ]
        };
        var viewModel = new JourneyViewModel(journey, new Project())
        {
            StationListViewMode = StationListViewMode.StopsOnly
        };

        viewModel.UpdateFromSessionState(new JourneySessionState
        {
            JourneyId = journey.Id,
            CurrentPos = 0,
            Counter = 1
        });

        Assert.That(viewModel.Stations.Single(station => station.Name == "Bielefeld").IsCurrentStation, Is.True);
        Assert.That(viewModel.Stations.Single(station => station.Name == "Herford").IsCurrentStation, Is.False);
    }

    [Test]
    public void UpdateFromSessionState_FullTimelineMode_HighlightsExactPosition()
    {
        var journey = new Journey
        {
            Id = Guid.NewGuid(),
            Stations =
            [
                new Station { Name = "Event1", IsVirtual = true },
                new Station { Name = "Bielefeld" }
            ]
        };
        var viewModel = new JourneyViewModel(journey, new Project())
        {
            StationListViewMode = StationListViewMode.FullTimeline
        };

        viewModel.UpdateFromSessionState(new JourneySessionState
        {
            JourneyId = journey.Id,
            CurrentPos = 0,
            Counter = 1
        });

        Assert.That(viewModel.Stations.Single(station => station.Name == "Event1").IsCurrentStation, Is.True);
        Assert.That(viewModel.Stations.Single(station => station.Name == "Bielefeld").IsCurrentStation, Is.False);
    }
}