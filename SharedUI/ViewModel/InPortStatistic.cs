// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Represents lap statistics for a single InPort (track).
/// Used by OverviewPage in WinUI, MAUI, and WebApp.
/// </summary>
public partial class InPortStatistic : ObservableObject
{
    [ObservableProperty]
    private int inPort;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private int count;

    [ObservableProperty]
    private int targetLapCount = 10;

    [ObservableProperty]
    private TimeSpan? lastLapTime;

    [ObservableProperty]
    private DateTime? lastFeedbackTime;

    [ObservableProperty]
    private bool hasReceivedFirstLap;

    /// <summary>
    /// Progress as percentage (0.0 to 1.0) for ProgressBar binding.
    /// </summary>
    public double Progress => TargetLapCount > 0 ? (double)Count / TargetLapCount : 0.0;

    /// <summary>
    /// Formatted last lap time for display (mm:ss or --:--).
    /// </summary>
    public string LastLapTimeFormatted => LastLapTime.HasValue
        ? $"{LastLapTime.Value.Minutes:D2}:{LastLapTime.Value.Seconds:D2}"
        : "--:--";

    /// <summary>
    /// Formatted last feedback time for display (HH:mm:ss or --:--:--).
    /// </summary>
    public string LastFeedbackTimeFormatted => LastFeedbackTime.HasValue
        ? LastFeedbackTime.Value.ToLocalTime().ToString("HH:mm:ss")
        : "--:--:--";

    /// <summary>
    /// Formatted lap count for display (X/Y laps format).
    /// </summary>
    public string LapCountFormatted => $"{Count}/{TargetLapCount} laps";

    /// <summary>
    /// Display name: Uses Name if available, otherwise falls back to "InPort X".
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : $"InPort {InPort}";

    /// <summary>
    /// Background color name for track card.
    /// Material Design inspired colors:
    /// - #EF5350 (Red 400): Soft red for "no activity" state
    /// - #66BB6A (Green 400): Bright green for "active" state
    /// </summary>
    public string BackgroundColorName => HasReceivedFirstLap ? "#66BB6A" : "#EF5350";

    partial void OnCountChanged(int value)
    {
        if (value > 0 && !HasReceivedFirstLap)
        {
            HasReceivedFirstLap = true;
        }
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LapCountFormatted));
    }

    partial void OnHasReceivedFirstLapChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(BackgroundColorName));
    }

    partial void OnTargetLapCountChanged(int value)
    {
        _ = value;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(LapCountFormatted));
    }

    partial void OnLastLapTimeChanged(TimeSpan? value)
    {
        _ = value;
        OnPropertyChanged(nameof(LastLapTimeFormatted));
    }

    partial void OnLastFeedbackTimeChanged(DateTime? value)
    {
        _ = value;
        OnPropertyChanged(nameof(LastFeedbackTimeFormatted));
    }
}
