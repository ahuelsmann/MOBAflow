// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interface;
using System.Globalization;

/// <summary>
/// Represents lap statistics for a single InPort (track).
/// Used by OverviewPage in WinUI, MAUI, and WebApp.
/// </summary>
/// <param name="commands">Optional local counter commands; without them the row is read-only.</param>
public partial class InPortStatistic(IRuntimeCommandGateway? commands = null) : ObservableObject
{
    private readonly IRuntimeCommandGateway? _commands = commands;

    /// <summary>Unsigned integer text entered for a counter correction.</summary>
    [ObservableProperty]
    public partial string CounterValue { get; set; } = string.Empty;

    /// <summary>Validation or command failure for this input's correction.</summary>
    [ObservableProperty]
    public partial string CounterError { get; set; } = string.Empty;

    /// <summary>Whether the input can be edited by this statistics view.</summary>
    public bool CanEditCounter => _commands is not null;

    [RelayCommand(CanExecute = nameof(CanEditCounter))]
    private async Task SetCounterAsync()
    {
        if (!ulong.TryParse(CounterValue?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            CounterError = "Enter a whole number from 0 to 18446744073709551615.";
            return;
        }

        await ChangeCounterAsync(() => _commands!.SetInPortCounterAsync(checked((uint)InPort), value)).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanEditCounter))]
    private Task ResetCounterAsync() =>
        ChangeCounterAsync(() => _commands!.ResetInPortCounterAsync(checked((uint)InPort)));

    private async Task ChangeCounterAsync(Func<Task> change)
    {
        CounterError = string.Empty;
        try
        {
            await change().ConfigureAwait(true);
            CounterValue = string.Empty;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            CounterError = ex.Message;
        }
    }

    [ObservableProperty]
    private int _inPort;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ulong _count;

    [ObservableProperty]
    private int _targetLapCount = 10;

    [ObservableProperty]
    private TimeSpan? _lastLapTime;

    [ObservableProperty]
    private DateTime? _lastFeedbackTime;

    [ObservableProperty]
    private bool _hasReceivedFirstLap;

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

    partial void OnCountChanged(ulong value)
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
