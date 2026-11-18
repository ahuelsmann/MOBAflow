// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Configuration;

/// <summary>
/// Page keys of the feature toggles in <see cref="FeatureToggleSettings.Pages"/>.
/// </summary>
public static class FeatureTogglePages
{
    /// <summary>Overview page key.</summary>
    public const string Overview = nameof(Overview);
    /// <summary>Solution page key.</summary>
    public const string Solution = nameof(Solution);
    /// <summary>Journeys page key.</summary>
    public const string Journeys = nameof(Journeys);
    /// <summary>Timetable page key.</summary>
    public const string Timetable = nameof(Timetable);
    /// <summary>Event Manager page key.</summary>
    public const string EventManager = nameof(EventManager);
    /// <summary>Workflows page key.</summary>
    public const string Workflows = nameof(Workflows);
    /// <summary>Stations page key.</summary>
    public const string Stations = nameof(Stations);
    /// <summary>Track Plan Editor page key.</summary>
    public const string TrackPlanEditor = nameof(TrackPlanEditor);
    /// <summary>Signal Box page key.</summary>
    public const string SignalBox = nameof(SignalBox);
    /// <summary>Journey Map page key.</summary>
    public const string JourneyMap = nameof(JourneyMap);
    /// <summary>Monitor page key.</summary>
    public const string Monitor = nameof(Monitor);
    /// <summary>Recorder page key.</summary>
    public const string Recorder = nameof(Recorder);
    /// <summary>Matrix Images page key.</summary>
    public const string Matrix = nameof(Matrix);
    /// <summary>ESP32 Display page key.</summary>
    public const string Display = nameof(Display);
    /// <summary>Locomotives page key.</summary>
    public const string Locomotives = nameof(Locomotives);
    /// <summary>Passenger Wagons page key.</summary>
    public const string PassengerWagons = nameof(PassengerWagons);
    /// <summary>Goods Wagons page key.</summary>
    public const string GoodsWagons = nameof(GoodsWagons);
    /// <summary>Trains page key.</summary>
    public const string Trains = nameof(Trains);
    /// <summary>Train Control page key.</summary>
    public const string TrainControl = nameof(TrainControl);
}

/// <summary>
/// Registered feature-toggle pages with their defaults. Every registered page is available unless the user
/// switches it off; a page without a stored label shows its default badge label.
/// </summary>
public static class FeatureToggleRegistry
{
    private const string Preview = "Preview";

    private static readonly Dictionary<string, string> DefaultLabels = new(StringComparer.Ordinal)
    {
        [FeatureTogglePages.Overview] = string.Empty,
        [FeatureTogglePages.Solution] = string.Empty,
        [FeatureTogglePages.Journeys] = string.Empty,
        [FeatureTogglePages.Timetable] = Preview,
        [FeatureTogglePages.EventManager] = Preview,
        [FeatureTogglePages.Workflows] = string.Empty,
        [FeatureTogglePages.Stations] = string.Empty,
        [FeatureTogglePages.TrackPlanEditor] = Preview,
        [FeatureTogglePages.SignalBox] = Preview,
        [FeatureTogglePages.JourneyMap] = Preview,
        [FeatureTogglePages.Monitor] = Preview,
        [FeatureTogglePages.Recorder] = string.Empty,
        [FeatureTogglePages.Matrix] = string.Empty,
        [FeatureTogglePages.Display] = string.Empty,
        [FeatureTogglePages.Locomotives] = string.Empty,
        [FeatureTogglePages.PassengerWagons] = Preview,
        [FeatureTogglePages.GoodsWagons] = Preview,
        [FeatureTogglePages.Trains] = Preview,
        [FeatureTogglePages.TrainControl] = Preview,
    };

    /// <summary>
    /// Keys of all registered feature-toggle pages.
    /// </summary>
    public static IReadOnlyCollection<string> PageKeys => DefaultLabels.Keys;

    /// <summary>
    /// Returns whether <paramref name="key"/> is a registered feature-toggle page.
    /// </summary>
    public static bool IsRegistered(string? key) => key is not null && DefaultLabels.ContainsKey(key);

    /// <summary>
    /// Reads a registered page toggle; a page without a stored entry is available.
    /// Returns false when <paramref name="key"/> is not a registered page.
    /// </summary>
    public static bool TryGetPageAvailability(FeatureToggleSettings settings, string key, out bool value)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!IsRegistered(key))
        {
            value = false;
            return false;
        }

        value = !settings.Pages.TryGetValue(key, out var page) || page is null || page.IsAvailable;
        return true;
    }

    /// <summary>
    /// Reads a registered page toggle. Unknown keys return <paramref name="defaultIfUnknown"/>.
    /// </summary>
    public static bool GetPageAvailabilityOrDefault(FeatureToggleSettings settings, string key, bool defaultIfUnknown = true) =>
        TryGetPageAvailability(settings, key, out var value) ? value : defaultIfUnknown;

    /// <summary>
    /// Writes a registered page toggle. Returns false when <paramref name="key"/> is not a registered page.
    /// </summary>
    public static bool TrySetPageAvailability(FeatureToggleSettings settings, string key, bool value)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!IsRegistered(key))
        {
            return false;
        }

        if (settings.Pages.TryGetValue(key, out var page) && page is not null)
        {
            page.IsAvailable = value;
        }
        else
        {
            settings.Pages[key] = new FeatureTogglePageSettings { IsAvailable = value };
        }

        return true;
    }

    /// <summary>
    /// Reads the badge label of a registered page: the stored label, otherwise the registry default.
    /// Returns null for unknown pages and for empty or whitespace labels.
    /// </summary>
    public static string? GetBadgeLabel(FeatureToggleSettings settings, string? key)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (key is null || !DefaultLabels.TryGetValue(key, out var defaultLabel))
        {
            return null;
        }

        var label = settings.Pages.TryGetValue(key, out var page) && page?.Label is not null
            ? page.Label
            : defaultLabel;
        return string.IsNullOrWhiteSpace(label) ? null : label;
    }
}
