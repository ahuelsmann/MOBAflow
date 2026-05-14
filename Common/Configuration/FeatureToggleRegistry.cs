// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Configuration;

/// <summary>
/// Explicit registry for feature-toggle page keys and badge label readers.
/// Replaces reflection on <see cref="FeatureToggleSettings"/> so renames are compile-time checked.
/// </summary>
public static class FeatureToggleRegistry
{
    /// <summary>
    /// Boolean page-availability properties on <see cref="FeatureToggleSettings"/> used as navigation toggle keys.
    /// </summary>
    public static readonly HashSet<string> PageAvailabilityKeys =
    [
        nameof(FeatureToggleSettings.IsOverviewPageAvailable),
        nameof(FeatureToggleSettings.IsSolutionPageAvailable),
        nameof(FeatureToggleSettings.IsJourneysPageAvailable),
        nameof(FeatureToggleSettings.IsWorkflowsPageAvailable),
        nameof(FeatureToggleSettings.IsTrackPlanEditorPageAvailable),
        nameof(FeatureToggleSettings.IsSignalBoxPageAvailable),
        nameof(FeatureToggleSettings.IsJourneyMapPageAvailable),
        nameof(FeatureToggleSettings.IsMonitorPageAvailable),
        nameof(FeatureToggleSettings.IsMatrixPageAvailable),
        nameof(FeatureToggleSettings.IsDisplayPageAvailable),
        nameof(FeatureToggleSettings.IsLocomotivesPageAvailable),
        nameof(FeatureToggleSettings.IsPassengerWagonsPageAvailable),
        nameof(FeatureToggleSettings.IsGoodsWagonsPageAvailable),
        nameof(FeatureToggleSettings.IsTrainsPageAvailable),
        nameof(FeatureToggleSettings.IsTrainControlPageAvailable),
    ];

    private static readonly Dictionary<string, (Func<FeatureToggleSettings, bool> Get, Action<FeatureToggleSettings, bool> Set)> PageAvailabilityAccessors =
        new(StringComparer.Ordinal)
        {
            [nameof(FeatureToggleSettings.IsOverviewPageAvailable)] = (s => s.IsOverviewPageAvailable, (s, v) => s.IsOverviewPageAvailable = v),
            [nameof(FeatureToggleSettings.IsSolutionPageAvailable)] = (s => s.IsSolutionPageAvailable, (s, v) => s.IsSolutionPageAvailable = v),
            [nameof(FeatureToggleSettings.IsJourneysPageAvailable)] = (s => s.IsJourneysPageAvailable, (s, v) => s.IsJourneysPageAvailable = v),
            [nameof(FeatureToggleSettings.IsWorkflowsPageAvailable)] = (s => s.IsWorkflowsPageAvailable, (s, v) => s.IsWorkflowsPageAvailable = v),
            [nameof(FeatureToggleSettings.IsTrackPlanEditorPageAvailable)] = (s => s.IsTrackPlanEditorPageAvailable, (s, v) => s.IsTrackPlanEditorPageAvailable = v),
            [nameof(FeatureToggleSettings.IsSignalBoxPageAvailable)] = (s => s.IsSignalBoxPageAvailable, (s, v) => s.IsSignalBoxPageAvailable = v),
            [nameof(FeatureToggleSettings.IsJourneyMapPageAvailable)] = (s => s.IsJourneyMapPageAvailable, (s, v) => s.IsJourneyMapPageAvailable = v),
            [nameof(FeatureToggleSettings.IsMonitorPageAvailable)] = (s => s.IsMonitorPageAvailable, (s, v) => s.IsMonitorPageAvailable = v),
            [nameof(FeatureToggleSettings.IsMatrixPageAvailable)] = (s => s.IsMatrixPageAvailable, (s, v) => s.IsMatrixPageAvailable = v),
            [nameof(FeatureToggleSettings.IsDisplayPageAvailable)] = (s => s.IsDisplayPageAvailable, (s, v) => s.IsDisplayPageAvailable = v),
            [nameof(FeatureToggleSettings.IsLocomotivesPageAvailable)] = (s => s.IsLocomotivesPageAvailable, (s, v) => s.IsLocomotivesPageAvailable = v),
            [nameof(FeatureToggleSettings.IsPassengerWagonsPageAvailable)] = (s => s.IsPassengerWagonsPageAvailable, (s, v) => s.IsPassengerWagonsPageAvailable = v),
            [nameof(FeatureToggleSettings.IsGoodsWagonsPageAvailable)] = (s => s.IsGoodsWagonsPageAvailable, (s, v) => s.IsGoodsWagonsPageAvailable = v),
            [nameof(FeatureToggleSettings.IsTrainsPageAvailable)] = (s => s.IsTrainsPageAvailable, (s, v) => s.IsTrainsPageAvailable = v),
            [nameof(FeatureToggleSettings.IsTrainControlPageAvailable)] = (s => s.IsTrainControlPageAvailable, (s, v) => s.IsTrainControlPageAvailable = v),
        };

    /// <summary>
    /// Reads a registered page-availability toggle. Returns false when <paramref name="key"/> is not a known page toggle.
    /// </summary>
    public static bool TryGetPageAvailability(FeatureToggleSettings settings, string key, out bool value)
    {
        if (!PageAvailabilityAccessors.TryGetValue(key, out var acc))
        {
            value = false;
            return false;
        }

        value = acc.Get(settings);
        return true;
    }

    /// <summary>
    /// Reads a registered page-availability toggle. Unknown keys return <paramref name="defaultIfUnknown"/> (legacy UI behavior: pages stay on unless explicitly registered).
    /// </summary>
    public static bool GetPageAvailabilityOrDefault(FeatureToggleSettings settings, string key, bool defaultIfUnknown = true)
    {
        if (PageAvailabilityAccessors.TryGetValue(key, out var acc))
            return acc.Get(settings);
        return defaultIfUnknown;
    }

    /// <summary>
    /// Writes a registered page-availability toggle. Returns false when <paramref name="key"/> is not a known page toggle.
    /// </summary>
    public static bool TrySetPageAvailability(FeatureToggleSettings settings, string key, bool value)
    {
        if (!PageAvailabilityAccessors.TryGetValue(key, out var acc))
            return false;
        acc.Set(settings, value);
        return true;
    }

    private static readonly Dictionary<string, Func<FeatureToggleSettings, string?>> BadgeLabelReaders =
        new(StringComparer.Ordinal)
        {
            [nameof(FeatureToggleSettings.OverviewPageLabel)] = ft => ft.OverviewPageLabel,
            [nameof(FeatureToggleSettings.SolutionPageLabel)] = ft => ft.SolutionPageLabel,
            [nameof(FeatureToggleSettings.JourneysPageLabel)] = ft => ft.JourneysPageLabel,
            [nameof(FeatureToggleSettings.WorkflowsPageLabel)] = ft => ft.WorkflowsPageLabel,
            [nameof(FeatureToggleSettings.TrackPlanEditorPageLabel)] = ft => ft.TrackPlanEditorPageLabel,
            [nameof(FeatureToggleSettings.SignalBoxPageLabel)] = ft => ft.SignalBoxPageLabel,
            [nameof(FeatureToggleSettings.JourneyMapPageLabel)] = ft => ft.JourneyMapPageLabel,
            [nameof(FeatureToggleSettings.MonitorPageLabel)] = ft => ft.MonitorPageLabel,
            [nameof(FeatureToggleSettings.MatrixPageLabel)] = ft => ft.MatrixPageLabel,
            [nameof(FeatureToggleSettings.DisplayPageLabel)] = ft => ft.DisplayPageLabel,
            [nameof(FeatureToggleSettings.LocomotivesPageLabel)] = ft => ft.LocomotivesPageLabel,
            [nameof(FeatureToggleSettings.PassengerWagonsPageLabel)] = ft => ft.PassengerWagonsPageLabel,
            [nameof(FeatureToggleSettings.GoodsWagonsPageLabel)] = ft => ft.GoodsWagonsPageLabel,
            [nameof(FeatureToggleSettings.TrainsPageLabel)] = ft => ft.TrainsPageLabel,
            [nameof(FeatureToggleSettings.TrainControlPageLabel)] = ft => ft.TrainControlPageLabel,
        };

    /// <summary>
    /// Reads a badge label string from <paramref name="settings"/> when <paramref name="badgeLabelPropertyName"/>
    /// is a known <see cref="FeatureToggleSettings"/> string property.
    /// </summary>
    public static string? GetBadgeLabel(FeatureToggleSettings settings, string? badgeLabelPropertyName)
    {
        if (string.IsNullOrEmpty(badgeLabelPropertyName)
            || !BadgeLabelReaders.TryGetValue(badgeLabelPropertyName, out var reader))
        {
            return null;
        }

        var value = reader(settings);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
