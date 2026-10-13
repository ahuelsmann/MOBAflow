// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.TrainControl;

using System.Globalization;

/// <summary>
/// Previous, current and next stop of the train-control timetable strip.
/// </summary>
[TestFixture]
internal sealed class JourneyStationProjectionTests
{
    private const string Dash = JourneyStationProjection.Placeholder;

    [Test]
    public void Describe_ShowsDashesWithoutAStation()
    {
        var display = JourneyStationProjection.Describe(null);

        Assert.That(display, Is.EqualTo(new StationDisplay(Dash, Dash, Dash, Dash, HasValue: false, IsExitOnLeft: false)));
    }

    [Test]
    public void Describe_ShowsTimesPlatformAndExitSide()
    {
        var platform2 = new Platform { Number = 2 };
        var platform5 = new Platform { Number = 5 };
        var station = new Station
        {
            Name = "Bielefeld",
            Arrival = new DateTime(2026, 10, 13, 8, 5, 0, DateTimeKind.Local),
            Departure = new DateTime(2026, 10, 13, 8, 7, 0, DateTimeKind.Local),
            Platforms = [platform2, platform5],
            PlatformId = platform5.Id,
            IsExitOnLeft = true,
        };

        var display = JourneyStationProjection.Describe(station);

        Assert.That(display, Is.EqualTo(new StationDisplay(
            "Bielefeld",
            station.Arrival.Value.ToString("HH:mm", CultureInfo.CurrentCulture),
            station.Departure.Value.ToString("HH:mm", CultureInfo.CurrentCulture),
            "5",
            HasValue: true,
            IsExitOnLeft: true)));
    }

    [Test]
    public void Describe_FallsBackToTheFirstPlatformAndDashesForMissingValues()
    {
        var withoutSelectedPlatform = new Station { Name = "Herford", Platforms = [new Platform { Number = 3 }] };
        var withoutPlatforms = new Station { Name = "Minden" };

        var first = JourneyStationProjection.Describe(withoutSelectedPlatform);
        var none = JourneyStationProjection.Describe(withoutPlatforms);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Track, Is.EqualTo("3"));
            Assert.That(first.Arrival, Is.EqualTo(Dash));
            Assert.That(none.Track, Is.EqualTo(Dash));
        }
    }

    [Test]
    public void PreviousCurrentNext_FollowTheStationIndex()
    {
        var journey = Journey("A", "B", "C");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Names(journey, 0), Is.EqualTo((Dash, "A", "B")));
            Assert.That(Names(journey, 1), Is.EqualTo(("A", "B", "C")));
            Assert.That(Names(journey, 2), Is.EqualTo(("B", "C", Dash)));
        }
    }

    [Test]
    public void PreviousCurrentNext_HandleIndexesOutsideTheJourney()
    {
        var journey = Journey("A", "B");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Names(journey, 5), Is.EqualTo(("B", Dash, Dash)));
            Assert.That(Names(journey, -1), Is.EqualTo((Dash, Dash, "A")));
            Assert.That(Names(null, 0), Is.EqualTo((Dash, Dash, Dash)));
            Assert.That(Names(Journey(), 0), Is.EqualTo((Dash, Dash, Dash)));
        }
    }

    private static (string Previous, string Current, string Next) Names(Journey? journey, int index) =>
        (JourneyStationProjection.Previous(journey, index).Name,
            JourneyStationProjection.Current(journey, index).Name,
            JourneyStationProjection.Next(journey, index).Name);

    private static Journey Journey(params string[] stations) =>
        new() { Stations = [.. stations.Select(name => new Station { Name = name })] };
}
