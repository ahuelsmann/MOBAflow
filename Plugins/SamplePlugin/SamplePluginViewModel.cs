// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.SharedUI.ViewModel;

namespace Moba.Plugin;

/// <summary>
/// ViewModel for the Sample Plugin Page.
/// Demonstrates MVVM pattern using CommunityToolkit.Mvvm.
/// Provides comprehensive project statistics and settings overview.
/// </summary>
public sealed partial class SamplePluginViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    #region Basic Properties
    [ObservableProperty]
    private string title = "Sample Plugin";

    [ObservableProperty]
    private string description = "Project statistics and settings overview - demonstrating plugin development.";

    [ObservableProperty]
    private string currentSolutionPath = "None";

    [ObservableProperty]
    private bool isZ21Connected;

    [ObservableProperty]
    private bool isTrackPowerOn;
    #endregion

    #region Project Statistics
    public string SolutionName => _mainWindowViewModel.Solution?.Name ?? "No Solution";
    public int ProjectCount => _mainWindowViewModel.Solution?.Projects.Count ?? 0;
    public string CurrentProjectName => _mainWindowViewModel.SelectedProject?.Name ?? "None";

    public int TotalJourneys => _mainWindowViewModel.SelectedProject?.Model.Journeys.Count ?? 0;
    public int TotalStations => _mainWindowViewModel.SelectedProject?.Model.Journeys.Sum(j => j.Stations?.Count ?? 0) ?? 0;
    public int TotalWorkflows => _mainWindowViewModel.SelectedProject?.Model.Workflows.Count ?? 0;
    public int TotalActions => _mainWindowViewModel.SelectedProject?.Model.Workflows.Sum(w => w.Actions?.Count ?? 0) ?? 0;
    public int TotalTrains => _mainWindowViewModel.SelectedProject?.Model.Trains.Count ?? 0;
    public int TotalLocomotives => _mainWindowViewModel.SelectedProject?.Model.Trains.Sum(t => t.LocomotiveIds?.Count ?? 0) ?? 0;
    public int TotalWagons => _mainWindowViewModel.SelectedProject?.Model.Trains.Sum(t => t.WagonIds?.Count ?? 0) ?? 0;
    public int TotalVoices => _mainWindowViewModel.SelectedProject?.Model.Voices.Count ?? 0;
    public int TotalSpeakerEngines => _mainWindowViewModel.SelectedProject?.Model.SpeakerEngines.Count ?? 0;
    #endregion

    #region Z21 Status
    public string Z21IpAddress => _mainWindowViewModel.IpAddress;
    public string Z21Port => _mainWindowViewModel.Port;
    public string Z21Status => IsZ21Connected ? "🟢 Connected" : "🔴 Disconnected";
    public string TrackPowerStatus => IsTrackPowerOn ? "⚡ ON" : "⭕ OFF";
    #endregion

    #region REST API Status
    public string RestApiPort => _mainWindowViewModel.RestApiPort.ToString();
    public string RestApiUrl => $"http://localhost:{_mainWindowViewModel.RestApiPort}";
    public bool IsRestApiEnabled => _mainWindowViewModel.AutoStartWebApp;
    public string RestApiStatus => IsRestApiEnabled ? "🟢 Enabled" : "⭕ Disabled";
    #endregion

    #region Settings Overview
    public string SpeechEngine => _mainWindowViewModel.SelectedSpeechEngine ?? "Not configured";
    public string SpeechRegion => _mainWindowViewModel.SpeechRegion;
    public bool IsAzureSpeech => _mainWindowViewModel.IsAzureSpeechEngineSelected;
    public bool AutoLoadLastSolution => _mainWindowViewModel.AutoLoadLastSolution;
    public bool AutoStartWebApp => _mainWindowViewModel.AutoStartWebApp;
    public bool HealthCheckEnabled => _mainWindowViewModel.HealthCheckEnabled;
    public double HealthCheckInterval => _mainWindowViewModel.HealthCheckIntervalSeconds;
    public double FeedbackPointCount => _mainWindowViewModel.CountOfFeedbackPoints;
    #endregion

    public SamplePluginViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        UpdateFromMainWindow();
        _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
    }

    private void UpdateFromMainWindow()
    {
        CurrentSolutionPath = _mainWindowViewModel.CurrentSolutionPath ?? "None";
        IsZ21Connected = _mainWindowViewModel.IsConnected;
        IsTrackPowerOn = _mainWindowViewModel.IsTrackPowerOn;
        RefreshAllStatistics();
    }

    [RelayCommand]
    private void RefreshStatistics() => RefreshAllStatistics();

    private void RefreshAllStatistics()
    {
        OnPropertyChanged(nameof(SolutionName));
        OnPropertyChanged(nameof(ProjectCount));
        OnPropertyChanged(nameof(CurrentProjectName));
        OnPropertyChanged(nameof(TotalJourneys));
        OnPropertyChanged(nameof(TotalStations));
        OnPropertyChanged(nameof(TotalWorkflows));
        OnPropertyChanged(nameof(TotalActions));
        OnPropertyChanged(nameof(TotalTrains));
        OnPropertyChanged(nameof(TotalLocomotives));
        OnPropertyChanged(nameof(TotalWagons));
        OnPropertyChanged(nameof(TotalVoices));
        OnPropertyChanged(nameof(TotalSpeakerEngines));
        OnPropertyChanged(nameof(Z21IpAddress));
        OnPropertyChanged(nameof(Z21Port));
        OnPropertyChanged(nameof(Z21Status));
        OnPropertyChanged(nameof(TrackPowerStatus));
        OnPropertyChanged(nameof(RestApiPort));
        OnPropertyChanged(nameof(RestApiUrl));
        OnPropertyChanged(nameof(IsRestApiEnabled));
        OnPropertyChanged(nameof(RestApiStatus));
        OnPropertyChanged(nameof(SpeechEngine));
        OnPropertyChanged(nameof(SpeechRegion));
        OnPropertyChanged(nameof(IsAzureSpeech));
        OnPropertyChanged(nameof(AutoLoadLastSolution));
        OnPropertyChanged(nameof(AutoStartWebApp));
        OnPropertyChanged(nameof(HealthCheckEnabled));
        OnPropertyChanged(nameof(HealthCheckInterval));
        OnPropertyChanged(nameof(FeedbackPointCount));
    }

    private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = sender;
        
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.CurrentSolutionPath):
                CurrentSolutionPath = _mainWindowViewModel.CurrentSolutionPath ?? "None";
                RefreshAllStatistics();
                break;
            case nameof(MainWindowViewModel.IsConnected):
                IsZ21Connected = _mainWindowViewModel.IsConnected;
                OnPropertyChanged(nameof(Z21Status));
                break;
            case nameof(MainWindowViewModel.IsTrackPowerOn):
                IsTrackPowerOn = _mainWindowViewModel.IsTrackPowerOn;
                OnPropertyChanged(nameof(TrackPowerStatus));
                break;
            case nameof(MainWindowViewModel.SelectedProject):
                RefreshAllStatistics();
                break;
        }
    }
}
