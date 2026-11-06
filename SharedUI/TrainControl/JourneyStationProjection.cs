// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.TrainControl;

using Domain;

using System.Globalization;

/// <summary>
/// How one stop appears in the train-control timetable strip.
/// </summary>
/// <param name="Name">Station name, or a dash without a station.</param>
/// <param name="Arrival">Arrival time (HH:mm), or a dash.</param>
/// <param name="Departure">Departure time (HH:mm), or a dash.</param>
/// <param name="Track">Platform number, or a dash.</param>
/// <param name="HasValue">Whether there is a station at this position.</param>
/// <param name="IsExitOnLeft">Whether passengers exit on the left.</param>
public sealed record StationDisplay(string Name, string Arrival, string Departure, string Track, bool HasValue, bool IsExitOnLeft);

/// <summary>
/// The previous, current and next stop of the selected journey as shown above the train-control desk.
/// </summary>
public static class JourneyStationProjection
{
    /// <summary>Shown when a value is missing.</summary>
    public const string Placeholder = "—";

    private const string TimeFormat = "HH:mm";

    /// <summary>The stop before <paramref name="currentIndex"/>; none at the first stop.</summary>
    public static StationDisplay Previous(Journey? journey, int currentIndex)
    {
        if (journey is null || journey.Stations.Count == 0)
        {
            return Describe(null);
        }

        var index = Math.Clamp(currentIndex, 0, journey.Stations.Count);
        return Describe(index <= 0 ? null : journey.Stations[index - 1]);
    }

    /// <summary>The stop at <paramref name="currentIndex"/>; none outside the journey.</summary>
    public static StationDisplay Current(Journey? journey, int currentIndex) => Describe(At(journey, currentIndex));

    /// <summary>The stop after <paramref name="currentIndex"/>; none after the last stop.</summary>
    public static StationDisplay Next(Journey? journey, int currentIndex) => Describe(At(journey, currentIndex + 1));

    /// <summary>Describes one stop; a missing stop shows dashes.</summary>
    public static StationDisplay Describe(Station? station)
    {
        if (station is null)
        {
            return new StationDisplay(Placeholder, Placeholder, Placeholder, Placeholder, HasValue: false, IsExitOnLeft: false);
        }

        return new StationDisplay(
            station.Name ?? Placeholder,
            station.Arrival?.ToString(TimeFormat, CultureInfo.CurrentCulture) ?? Placeholder,
            station.Departure?.ToString(TimeFormat, CultureInfo.CurrentCulture) ?? Placeholder,
            ResolvePlatform(station),
            HasValue: true,
            station.IsExitOnLeft);
    }

    private static Station? At(Journey? journey, int index) =>
        journey is null || index < 0 || index >= journey.Stations.Count ? null : journey.Stations[index];

    private static string ResolvePlatform(Station station) =>
        station.ResolvePlatform()?.Number.ToString(CultureInfo.CurrentCulture) ?? Placeholder;
}
