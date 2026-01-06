// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.SharedUI.ViewModel;

namespace Moba.Plugin;

/// <summary>
/// ViewModel for the Sample Plugin Dashboard.
/// Provides comprehensive statistics, settings, and connection status from MainWindowViewModel.
/// </summary>
public sealed partial class SamplePluginViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;

    public SamplePluginViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _main = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        _main.PropertyChanged += (s, e) => RefreshAll();
    }

    #region Solution & Project Statistics
    public string SolutionName => _main.Solution?.Name ?? "No Solution Loaded";
    public int ProjectCount => _main.Solution?.Projects.Count ?? 0;
    public string SelectedProjectName => _main.SelectedProject?.Name ?? "None";
    #endregion

    #region Entity Counts
    public int TotalJourneys => _main.SelectedProject?.Model.Journeys.Count ?? 0;
    public int TotalWorkflows => _main.SelectedProject?.Model.Workflows.Count ?? 0;
    public int TotalTrains => _main.SelectedProject?.Model.Trains.Count ?? 0;
    public int TotalLocomotives => _main.SelectedProject?.Model.Locomotives.Count ?? 0;
    public int TotalPassengerWagons => _main.SelectedProject?.Model.PassengerWagons.Count ?? 0;
    public int TotalGoodsWagons => _main.SelectedProject?.Model.GoodsWagons.Count ?? 0;
    #endregion

    #region Z21 Connection Status
    public bool IsZ21Connected => _main.IsConnected;
    public string Z21StatusText => _main.IsConnected ? "Connected" : "Disconnected";
    public string Z21StatusIcon => _main.IsConnected ? "🟢" : "🔴";
    public string Z21IpAddress => _main.IpAddress ?? "-";
    public string Z21Port => _main.Port ?? "21105";
    public bool IsTrackPowerOn => _main.IsTrackPowerOn;
    public string TrackPowerText => _main.IsTrackPowerOn ? "ON" : "OFF";
    public string TrackPowerIcon => _main.IsTrackPowerOn ? "⚡" : "⭕";
    
    // Z21 System State (if available)
    public string Z21MainCurrent => $"{_main.MainCurrent:F1} A";
    public string Z21Temperature => $"{_main.Temperature:F0} °C";
    public string Z21SupplyVoltage => $"{_main.SupplyVoltage:F1} V";
    #endregion

    #region REST API Status
    public bool IsRestApiRunning => _main.Settings?.Application.AutoStartWebApp ?? false;
    public string RestApiStatusText => IsRestApiRunning ? "Running" : "Stopped";
    public string RestApiStatusIcon => IsRestApiRunning ? "🟢" : "🔴";
    public int RestApiPort => _main.Settings?.RestApi.Port ?? 5001;
    public string LocalIpAddress => _main.LocalIpAddress;
    public string RestApiUrl => $"http://{LocalIpAddress}:{RestApiPort}";
    #endregion

    #region Settings Overview
    public bool AutoLoadLastSolution => _main.Settings?.Application.AutoLoadLastSolution ?? false;
    public bool AutoStartWebApp => _main.Settings?.Application.AutoStartWebApp ?? false;
    public string SpeechEngine => _main.SelectedSpeechEngine ?? "Not configured";
    public int SpeechRate => _main.SpeechRate;
    public double SpeechVolume => _main.SpeechVolume;
    public bool HealthCheckEnabled => _main.Settings?.HealthCheck.Enabled ?? false;
    public int HealthCheckInterval => _main.Settings?.HealthCheck.IntervalSeconds ?? 30;
    #endregion

    [RelayCommand]
    private void RefreshAll()
    {
        // Solution & Project
        OnPropertyChanged(nameof(SolutionName));
        OnPropertyChanged(nameof(ProjectCount));
        OnPropertyChanged(nameof(SelectedProjectName));

        // Entity Counts
        OnPropertyChanged(nameof(TotalJourneys));
        OnPropertyChanged(nameof(TotalWorkflows));
        OnPropertyChanged(nameof(TotalTrains));
        OnPropertyChanged(nameof(TotalLocomotives));
        OnPropertyChanged(nameof(TotalPassengerWagons));
        OnPropertyChanged(nameof(TotalGoodsWagons));

        // Z21 Connection
        OnPropertyChanged(nameof(IsZ21Connected));
        OnPropertyChanged(nameof(Z21StatusText));
        OnPropertyChanged(nameof(Z21StatusIcon));
        OnPropertyChanged(nameof(Z21IpAddress));
        OnPropertyChanged(nameof(Z21Port));
        OnPropertyChanged(nameof(IsTrackPowerOn));
        OnPropertyChanged(nameof(TrackPowerText));
        OnPropertyChanged(nameof(TrackPowerIcon));
        OnPropertyChanged(nameof(Z21MainCurrent));
        OnPropertyChanged(nameof(Z21Temperature));
        OnPropertyChanged(nameof(Z21SupplyVoltage));

        // REST API
        OnPropertyChanged(nameof(IsRestApiRunning));
        OnPropertyChanged(nameof(RestApiStatusText));
        OnPropertyChanged(nameof(RestApiStatusIcon));
        OnPropertyChanged(nameof(RestApiPort));
        OnPropertyChanged(nameof(LocalIpAddress));
        OnPropertyChanged(nameof(RestApiUrl));

        // Settings
        OnPropertyChanged(nameof(AutoLoadLastSolution));
        OnPropertyChanged(nameof(AutoStartWebApp));
        OnPropertyChanged(nameof(SpeechEngine));
        OnPropertyChanged(nameof(SpeechRate));
        OnPropertyChanged(nameof(SpeechVolume));
        OnPropertyChanged(nameof(HealthCheckEnabled));
        OnPropertyChanged(nameof(HealthCheckInterval));
    }
}
