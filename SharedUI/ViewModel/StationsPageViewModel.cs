// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Common.Configuration;
using Common.Extension;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>Owns project station/platform editing and the station page layout using the shared session.</summary>
public sealed partial class StationsPageViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ILogger<StationsPageViewModel> _logger;
    private ProjectViewModel? _observedProject;
    private bool _disposed;
    private bool _isStationsListExpanded;
    private bool _isPlatformsListExpanded;
    private bool _isStationsPropertiesExpanded;
    private bool _isWorkflowLibraryVisible;

    public StationsPageViewModel(ISolutionSession session, IConnectionRuntime runtimeConnection,
        ISettingsService settingsService, LayoutColumnWidthsViewModel layoutColumnWidths,
        WorkflowLibraryViewModel workflowLibrary, ILogger<StationsPageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(workflowLibrary);
        ArgumentNullException.ThrowIfNull(logger);
        _session = session;
        _runtimeConnection = runtimeConnection;
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();
        _logger = logger;
        LayoutColumnWidths = layoutColumnWidths;
        WorkflowLibrary = workflowLibrary;
        _isStationsListExpanded = _settings.Layout.StationsPage.IsStationsListExpanded;
        _isPlatformsListExpanded = _settings.Layout.StationsPage.IsPlatformsListExpanded;
        _isStationsPropertiesExpanded = _settings.Layout.StationsPage.IsPropertiesExpanded;
        _isWorkflowLibraryVisible = _settings.Layout.StationsPage.IsWorkflowLibraryExpanded;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.SolutionReplacing += OnSolutionReplacing;
        ObserveProject(SelectedProject);
    }

    public ProjectViewModel? SelectedProject => _session.SelectedProject;
    public LayoutColumnWidthsViewModel LayoutColumnWidths { get; }
    public WorkflowLibraryViewModel WorkflowLibrary { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectStationCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddPlatformToProjectStationCommand))]
    public partial StationViewModel? SelectedProjectStation { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectStationPlatformCommand))]
    public partial PlatformViewModel? SelectedProjectStationPlatform { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StationsPagePropertiesTitle))]
    public partial object? StationsPageSelectedObject { get; set; }

    public string StationsPagePropertiesTitle => StationsPageSelectedObject switch
    {
        StationViewModel => "Station Properties",
        PlatformViewModel => "Platform Properties",
        _ => "Properties"
    };

    partial void OnSelectedProjectStationChanged(StationViewModel? oldValue, StationViewModel? newValue)
    {
        SelectedProjectStationPlatform = null;
        DetachStation(oldValue);
        StationsPageSelectedObject = newValue;
        if (newValue is not null && !_disposed)
        {
            _session.TrackChanges(newValue);
            newValue.Platforms.CollectionChanged += OnPlatformsChanged;
        }
    }

    partial void OnSelectedProjectStationPlatformChanged(PlatformViewModel? oldValue, PlatformViewModel? newValue)
    {
        if (oldValue is not null) _session.UntrackChanges(oldValue);
        StationsPageSelectedObject = (object?)newValue ?? SelectedProjectStation;
        if (newValue is not null && !_disposed) _session.TrackChanges(newValue);
    }

    private void DetachStation(StationViewModel? station)
    {
        if (station is null) return;
        station.Platforms.CollectionChanged -= OnPlatformsChanged;
        _session.UntrackChanges(station);
    }

    private void OnPlatformsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedProjectStationPlatform is { } platform
            && SelectedProjectStation?.Platforms.Contains(platform) != true)
            SelectedProjectStationPlatform = null;
    }

    private void OnStationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedProjectStation is { } station && _observedProject?.Stations.Contains(station) != true)
            SelectedProjectStation = null;
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ISolutionSession.SelectedProject)) return;
        ObserveProject(SelectedProject);
        OnPropertyChanged(nameof(SelectedProject));
        AddProjectStationCommand.NotifyCanExecuteChanged();
    }

    private void OnSolutionReplacing(object? sender, EventArgs e)
    {
        SelectedProjectStation = null;
        SelectedProjectStationPlatform = null;
        StationsPageSelectedObject = null;
    }

    private void ObserveProject(ProjectViewModel? project)
    {
        if (_observedProject is not null) _observedProject.Stations.CollectionChanged -= OnStationsChanged;
        SelectedProjectStation = null;
        SelectedProjectStationPlatform = null;
        StationsPageSelectedObject = null;
        _observedProject = project;
        if (project is not null) project.Stations.CollectionChanged += OnStationsChanged;
    }

    private void RefreshProjectRuntimeSnapshot()
    {
        if (SelectedProject is { } project)
            _runtimeConnection.UpdateProjectAsync(project.Model)
                .Observe(ex => LogOperationFailed(_logger, "Update project runtime", ex));
    }

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        _settingsService.SaveSettingsAsync(_settings)
            .Observe(ex => LogOperationFailed(_logger, "Persist station layout", ex));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _session.SolutionReplacing -= OnSolutionReplacing;
        if (_observedProject is not null) _observedProject.Stations.CollectionChanged -= OnStationsChanged;
        DetachStation(SelectedProjectStation);
        if (SelectedProjectStationPlatform is { } platform) _session.UntrackChanges(platform);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogOperationFailed(ILogger logger, string operationName, Exception exception);

    public bool IsStationsListExpanded
    {
        get => _isStationsListExpanded;
        set
        {
            if (SetProperty(ref _isStationsListExpanded, value))
                PersistLayoutState(layout => layout.StationsPage.IsStationsListExpanded = value);
        }
    }

    public bool IsPlatformsListExpanded
    {
        get => _isPlatformsListExpanded;
        set
        {
            if (SetProperty(ref _isPlatformsListExpanded, value))
                PersistLayoutState(layout => layout.StationsPage.IsPlatformsListExpanded = value);
        }
    }

    public bool IsStationsPropertiesExpanded
    {
        get => _isStationsPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isStationsPropertiesExpanded, value))
                PersistLayoutState(layout => layout.StationsPage.IsPropertiesExpanded = value);
        }
    }

    public bool IsWorkflowLibraryVisible
    {
        get => _isWorkflowLibraryVisible;
        set
        {
            if (SetProperty(ref _isWorkflowLibraryVisible, value))
                PersistLayoutState(layout => layout.StationsPage.IsWorkflowLibraryExpanded = value);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddProjectStation))]
    private void AddProjectStation()
    {
        if (SelectedProject == null) return;

        var station = new Station { Name = "New Station" };
        station.Platforms.Add(new Platform { Name = "Platform 1", Number = 1 });
        SelectedProject.Model.Stations.Add(station);
        var stationVm = new StationViewModel(station, SelectedProject.Model);
        SelectedProject.Stations.Add(stationVm);
        SelectedProjectStation = stationVm;
        RefreshProjectRuntimeSnapshot();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProjectStation))]
    private void DeleteProjectStation()
    {
        if (SelectedProject == null || SelectedProjectStation == null) return;

        SelectedProject.Model.Stations.Remove(SelectedProjectStation.Model);
        SelectedProject.Stations.Remove(SelectedProjectStation);
        SelectedProjectStation = null;
        SelectedProjectStationPlatform = null;
        RefreshProjectRuntimeSnapshot();
    }

    [RelayCommand(CanExecute = nameof(CanAddPlatformToProjectStation))]
    private void AddPlatformToProjectStation()
    {
        if (SelectedProject == null || SelectedProjectStation == null) return;

        var platform = new Platform
        {
            Name = $"Platform {SelectedProjectStation.Model.Platforms.Count + 1}",
            Number = (uint)(SelectedProjectStation.Model.Platforms.Count + 1)
        };
        SelectedProjectStation.Model.Platforms.Add(platform);
        SelectedProjectStation.RefreshPlatforms();
        SelectedProjectStationPlatform = SelectedProjectStation.Platforms.LastOrDefault();
        RefreshProjectRuntimeSnapshot();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProjectStationPlatform))]
    private void DeleteProjectStationPlatform()
    {
        if (SelectedProject == null || SelectedProjectStation == null || SelectedProjectStationPlatform == null) return;

        SelectedProjectStation.Model.Platforms.Remove(SelectedProjectStationPlatform.Model);
        if (SelectedProjectStation.Model.PlatformId == SelectedProjectStationPlatform.Model.Id)
        {
            SelectedProjectStation.PlatformId = null;
        }

        SelectedProjectStation.RefreshPlatforms();
        SelectedProjectStationPlatform = null;
        RefreshProjectRuntimeSnapshot();
    }

    private bool CanAddProjectStation() => SelectedProject != null;

    private bool CanDeleteProjectStation() => SelectedProjectStation != null;

    private bool CanAddPlatformToProjectStation() => SelectedProjectStation != null;

    private bool CanDeleteProjectStationPlatform() => SelectedProjectStationPlatform != null;
}
