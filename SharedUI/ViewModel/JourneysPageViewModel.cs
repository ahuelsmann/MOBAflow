// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Common.Configuration;
using Common.Extension;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Helper;
using Interface;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>Owns journey/station editing, the city library and page layout using the shared solution session.</summary>
public sealed partial class JourneysPageViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly IRuntimeCommandGateway _runtimeCommandGateway;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ICityService _cityLibraryService;
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ILogger<JourneysPageViewModel> _logger;
    private ProjectViewModel? _observedProject;
    private JourneyViewModel? _observedJourney;
    private bool _disposed;
    private bool _isJourneyListExpanded;
    private bool _isStationListExpanded;
    private bool _isCityLibraryVisible;
    private bool _isJourneyPropertiesExpanded;

    public JourneysPageViewModel(ISolutionSession session, IConnectionRuntime runtimeConnection,
        IRuntimeCommandGateway runtimeCommandGateway, IUiDispatcher uiDispatcher, ICityService cityLibraryService,
        ISettingsService settingsService, LayoutColumnWidthsViewModel layoutColumnWidths,
        ILogger<JourneysPageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(runtimeCommandGateway);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(cityLibraryService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(logger);
        _session = session;
        _runtimeConnection = runtimeConnection;
        _runtimeCommandGateway = runtimeCommandGateway;
        _uiDispatcher = uiDispatcher;
        _cityLibraryService = cityLibraryService;
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();
        LayoutColumnWidths = layoutColumnWidths;
        _logger = logger;
        _isJourneyListExpanded = _settings.Layout.JourneysPage.IsJourneyListExpanded;
        _isStationListExpanded = _settings.Layout.JourneysPage.IsStationListExpanded;
        _isCityLibraryVisible = _settings.Layout.JourneysPage.IsCityLibraryExpanded;
        _isJourneyPropertiesExpanded = _settings.Layout.JourneysPage.IsJourneyPropertiesExpanded;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.SolutionReplacing += OnSolutionReplacing;
        ObserveProject(SelectedProject);
        if (SelectedJourney is null || SelectedProject?.Journeys.Contains(SelectedJourney) != true)
            SelectedJourney = SelectedProject?.Journeys.FirstOrDefault();
        ObserveJourney(SelectedJourney);
    }

    public ProjectViewModel? SelectedProject => _session.SelectedProject;
    public LayoutColumnWidthsViewModel LayoutColumnWidths { get; }

    /// <summary>The session remains the canonical selection shared with the map and event editor.</summary>
    public JourneyViewModel? SelectedJourney
    {
        get => _session.SelectedJourney;
        set => _session.SelectedJourney = value;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteStationCommand))]
    public partial StationViewModel? SelectedStation { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(JourneysPagePropertiesTitle))]
    public partial object? JourneysPageSelectedObject { get; set; }

    public string JourneysPagePropertiesTitle => JourneysPageSelectedObject switch
    {
        JourneyViewModel => "Journey Properties",
        StationViewModel => "Station Properties",
        _ => "Properties"
    };

    [RelayCommand]
    private void JourneysPageItemClicked(object? item) => JourneysPageSelectedObject = item;

    partial void OnSelectedStationChanged(StationViewModel? oldValue, StationViewModel? newValue)
    {
        if (oldValue is not null) _session.UntrackChanges(oldValue);
        if (ReferenceEquals(JourneysPageSelectedObject, oldValue)) JourneysPageSelectedObject = null;
        if (newValue is not null && !_disposed) _session.TrackChanges(newValue);
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            ObserveProject(SelectedProject);
            SelectedJourney = SelectedProject?.Journeys.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedProject));
            OnPropertyChanged(nameof(FilteredJourneys));
        }
        else if (e.PropertyName == nameof(ISolutionSession.SelectedJourney))
        {
            ObserveJourney(SelectedJourney);
            OnPropertyChanged(nameof(SelectedJourney));
            DeleteJourneyCommand.NotifyCanExecuteChanged();
            AddStationCommand.NotifyCanExecuteChanged();
            AddStationFromCityCommand.NotifyCanExecuteChanged();
            ResetJourneyCommand.NotifyCanExecuteChanged();
        }
    }

    private void ObserveProject(ProjectViewModel? project)
    {
        if (_observedProject is not null) _observedProject.Journeys.CollectionChanged -= OnJourneysChanged;
        _observedProject = project;
        if (project is not null) project.Journeys.CollectionChanged += OnJourneysChanged;
    }

    private void ObserveJourney(JourneyViewModel? journey)
    {
        if (_observedJourney is not null) _observedJourney.Stations.CollectionChanged -= OnStationsChanged;
        SelectedStation = null;
        JourneysPageSelectedObject = null;
        _observedJourney = journey;
        if (journey is not null) journey.Stations.CollectionChanged += OnStationsChanged;
        // The session owns journey property tracking, including auto-save and runtime refresh.
    }

    private void OnJourneysChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedJourney is { } journey && _observedProject?.Journeys.Contains(journey) != true)
            SelectedJourney = null;
        OnPropertyChanged(nameof(FilteredJourneys));
    }

    private void OnStationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedStation is { } station && _observedJourney?.Stations.Contains(station) != true)
            SelectedStation = null;
    }

    private void OnSolutionReplacing(object? sender, EventArgs e)
    {
        SelectedStation = null;
        JourneysPageSelectedObject = null;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredJourneys))]
    public partial string JourneySearchText { get; set; } = string.Empty;

    public IReadOnlyList<JourneyViewModel> FilteredJourneys => SelectedProject is { } project
        ? [.. project.Journeys.Where(journey => string.IsNullOrWhiteSpace(JourneySearchText)
            || journey.Name.Contains(JourneySearchText, StringComparison.OrdinalIgnoreCase))]
        : [];

    [RelayCommand]
    private void AddJourney()
    {
        if (SelectedProject is not { } project) return;
        SelectedJourney = EntityEditorHelper.AddEntity(project.Model.Journeys, project.Journeys,
            () => new Journey { Name = "New Journey" },
            model => new JourneyViewModel(model, project.Model, _uiDispatcher));
        RefreshProjectRuntimeSnapshot();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteJourney))]
    private void DeleteJourney()
    {
        if (SelectedProject is not { } project) return;
        EntityEditorHelper.DeleteEntity(SelectedJourney, project.Model.Journeys, project.Journeys,
            () => SelectedJourney = null);
        RefreshProjectRuntimeSnapshot();
    }

    private bool CanDeleteJourney() => SelectedJourney != null;

    [RelayCommand(CanExecute = nameof(CanAddStation))]
    private void AddStation()
    {
        if (SelectedJourney is null || SelectedProject is null) return;
        AddStationToSelectedJourney("New Station");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteStation))]
    private void DeleteStation()
    {
        if (SelectedJourney is not { } journey || SelectedStation is not { } station) return;
        journey.Model.Stations.Remove(station.Model);
        journey.RefreshStations();
        SelectedStation = null;
    }

    private bool CanAddStation() => SelectedJourney != null;
    private bool CanDeleteStation() => SelectedStation != null;

    [RelayCommand(CanExecute = nameof(CanResetJourney))]
    private async Task ResetJourney()
    {
        if (SelectedJourney is not { } journey) return;
        await _runtimeCommandGateway.ResetJourneyAsync(journey.Id).ConfigureAwait(true);
    }

    private bool CanResetJourney() => SelectedJourney != null;

    [ObservableProperty]
    public partial string JourneyCommandStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<City> CityLibrary { get; set; } = [];

    [ObservableProperty]
    public partial string CitySearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial City? SelectedCity { get; set; }

    partial void OnCitySearchTextChanged(string value) =>
        CityLibrary = new ObservableCollection<City>(_cityLibraryService.FilterCities(value));

    /// <summary>Loads read-only master data; the host calls this after the master data store is ready.</summary>
    public async Task LoadCityLibraryAsync()
    {
        var cities = await _cityLibraryService.LoadCitiesAsync().ConfigureAwait(false);
        _uiDispatcher.InvokeOnUi(() =>
        {
            if (!_disposed) CityLibrary = new ObservableCollection<City>(cities);
        });
    }

    [RelayCommand(CanExecute = nameof(CanAddStationFromCity))]
    private void AddStationFromCity(City? city)
    {
        if (city?.Stations.FirstOrDefault() is { } station)
            AddStationToSelectedJourney(station.Name);
    }

    private bool CanAddStationFromCity() => SelectedJourney != null;

    [RelayCommand]
    private void AddStationToJourney() => AddStationFromCity(SelectedCity);

    private void AddStationToSelectedJourney(string name)
    {
        if (SelectedJourney is not { } journey) return;
        journey.Model.Stations.Add(new Station { Name = name, IsExitOnLeft = false });
        journey.RefreshStations();
        SelectedStation = journey.Stations.LastOrDefault();
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
            .Observe(ex => LogOperationFailed(_logger, "Persist journey layout", ex));
    }

    public bool IsJourneyListExpanded
    {
        get => _isJourneyListExpanded;
        set
        {
            if (SetProperty(ref _isJourneyListExpanded, value))
                PersistLayoutState(layout => layout.JourneysPage.IsJourneyListExpanded = value);
        }
    }

    public bool IsStationListExpanded
    {
        get => _isStationListExpanded;
        set
        {
            if (SetProperty(ref _isStationListExpanded, value))
                PersistLayoutState(layout => layout.JourneysPage.IsStationListExpanded = value);
        }
    }

    public bool IsCityLibraryVisible
    {
        get => _isCityLibraryVisible;
        set
        {
            if (SetProperty(ref _isCityLibraryVisible, value))
                PersistLayoutState(layout => layout.JourneysPage.IsCityLibraryExpanded = value);
        }
    }

    public bool IsJourneyPropertiesExpanded
    {
        get => _isJourneyPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isJourneyPropertiesExpanded, value))
                PersistLayoutState(layout => layout.JourneysPage.IsJourneyPropertiesExpanded = value);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _session.SolutionReplacing -= OnSolutionReplacing;
        if (_observedProject is not null) _observedProject.Journeys.CollectionChanged -= OnJourneysChanged;
        if (_observedJourney is not null) _observedJourney.Stations.CollectionChanged -= OnStationsChanged;
        if (SelectedStation is { } station) _session.UntrackChanges(station);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogOperationFailed(ILogger logger, string operationName, Exception exception);
}
