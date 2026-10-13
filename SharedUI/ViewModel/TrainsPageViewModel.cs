// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Common.Configuration;
using Common.Extension;
using Interface;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Helper;

using System.Collections.ObjectModel;

/// <summary>Owns train selection, composition and layout using the shared project session and stock editors.</summary>
public sealed partial class TrainsPageViewModel : ObservableObject, IDisposable
{

    private readonly ISolutionSession _session;
    private readonly IConnectionRuntime _runtimeConnection;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TrainsPageViewModel> _logger;
    private bool _disposed;
    private bool _isTrainsListExpanded;
    private bool _isTrainsLocomotiveLibraryExpanded;
    private bool _isTrainsPassengerLibraryExpanded;
    private bool _isTrainsGoodsLibraryExpanded;
    private bool _isTrainsPropertiesExpanded;

    public TrainsPageViewModel(ISolutionSession session, IConnectionRuntime runtimeConnection,
        ISettingsService settingsService, LayoutColumnWidthsViewModel layoutColumnWidths,
        LocomotivesPageViewModel locomotivesPage, WagonsPageViewModel wagonsPage,
        ILogger<TrainsPageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(runtimeConnection);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(locomotivesPage);
        ArgumentNullException.ThrowIfNull(wagonsPage);
        ArgumentNullException.ThrowIfNull(logger);
        _session = session;
        _runtimeConnection = runtimeConnection;
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();
        _logger = logger;
        LayoutColumnWidths = layoutColumnWidths;
        LocomotivesPage = locomotivesPage;
        WagonsPage = wagonsPage;
        _isTrainsListExpanded = _settings.Layout.TrainsPage.IsTrainListExpanded;
        _isTrainsLocomotiveLibraryExpanded = _settings.Layout.TrainsPage.IsLocomotiveLibraryExpanded;
        _isTrainsPassengerLibraryExpanded = _settings.Layout.TrainsPage.IsPassengerLibraryExpanded;
        _isTrainsGoodsLibraryExpanded = _settings.Layout.TrainsPage.IsGoodsLibraryExpanded;
        _isTrainsPropertiesExpanded = _settings.Layout.TrainsPage.IsPropertiesExpanded;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.SolutionReplacing += OnSolutionReplacing;
        SelectedTrain = SelectedProject?.Trains.FirstOrDefault();
    }

    public ProjectViewModel? SelectedProject => _session.SelectedProject;
    public LayoutColumnWidthsViewModel LayoutColumnWidths { get; }
    public LocomotivesPageViewModel LocomotivesPage { get; }
    public WagonsPageViewModel WagonsPage { get; }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ISolutionSession.SelectedProject)) return;
        SelectedTrain = SelectedProject?.Trains.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedProject));
        OnPropertyChanged(nameof(FilteredTrains));
    }

    private void OnSolutionReplacing(object? sender, EventArgs e)
    {
        SelectedTrain = null;
        SelectedVehicle = null;
    }

    private void SaveChanges() => _session.SaveSolutionInternalAsync()
        .Observe(ex => LogOperationFailed(_logger, "Auto-save solution", ex));

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        _settingsService.SaveSettingsAsync(_settings)
            .Observe(ex => LogOperationFailed(_logger, "Persist train layout", ex));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _session.SolutionReplacing -= OnSolutionReplacing;
        if (SelectedTrain is { } train)
            train.VehiclesModified -= SelectedTrain_VehiclesModified;
        // The session owns train property tracking; detaching it here would stop other editors' auto-save.
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogOperationFailed(ILogger logger, string operationName, Exception exception);

    public bool IsTrainsListExpanded
    {
        get => _isTrainsListExpanded;
        set
        {
            if (SetProperty(ref _isTrainsListExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsTrainListExpanded = value);
        }
    }

    public bool IsTrainsLocomotiveLibraryExpanded
    {
        get => _isTrainsLocomotiveLibraryExpanded;
        set
        {
            if (SetProperty(ref _isTrainsLocomotiveLibraryExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsLocomotiveLibraryExpanded = value);
        }
    }

    public bool IsTrainsPassengerLibraryExpanded
    {
        get => _isTrainsPassengerLibraryExpanded;
        set
        {
            if (SetProperty(ref _isTrainsPassengerLibraryExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsPassengerLibraryExpanded = value);
        }
    }

    public bool IsTrainsGoodsLibraryExpanded
    {
        get => _isTrainsGoodsLibraryExpanded;
        set
        {
            if (SetProperty(ref _isTrainsGoodsLibraryExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsGoodsLibraryExpanded = value);
        }
    }

    public bool IsTrainsPropertiesExpanded
    {
        get => _isTrainsPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isTrainsPropertiesExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsPropertiesExpanded = value);
        }
    }

    private static readonly ObservableCollection<VehicleItemViewModel> EmptyVehicleItems = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteTrainCommand))]
    public partial TrainViewModel? SelectedTrain { get; set; }

    [ObservableProperty]
    public partial VehicleItemViewModel? SelectedVehicle { get; set; }

    [ObservableProperty]
    public partial string TrainSearchText { get; set; } = string.Empty;

    partial void OnSelectedTrainChanging(TrainViewModel? value)
    {
        _ = value;
        if (SelectedTrain != null)
        {
            SelectedTrain.VehiclesModified -= SelectedTrain_VehiclesModified;
        }
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        SelectedVehicle = null;

        if (value != null && !_disposed)
        {
            _session.TrackChanges(value);
            value.VehiclesModified += SelectedTrain_VehiclesModified;
            value.RefreshVehicleItems();
        }

        OnPropertyChanged(nameof(SelectedVehicles));
        OnPropertyChanged(nameof(TrainsCompositionTitle));
    }

    private void SelectedTrain_VehiclesModified(object? sender, EventArgs e)
    {
        if (_session.IsAutoSaveSuppressed)
        {
            return;
        }

        SaveChanges();
        RefreshProjectRuntimeSnapshot();
    }

    /// <summary>
    /// Refreshes the runtime's snapshot of the selected project after an editor change, without re-activation.
    /// </summary>
    private void RefreshProjectRuntimeSnapshot()
    {
        if (SelectedProject is { } project)
        {
            _runtimeConnection.UpdateProjectAsync(project.Model).Observe(ex => LogOperationFailed(_logger, "Update project runtime", ex));
        }
    }

    partial void OnTrainSearchTextChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FilteredTrains));
    }

    public ObservableCollection<VehicleItemViewModel> SelectedVehicles => SelectedTrain?.VehicleItems ?? EmptyVehicleItems;

    public string TrainsCompositionTitle => SelectedTrain == null
        ? "Train Composition"
        : $"Train Composition: {SelectedTrain.Name}";

    public IReadOnlyList<TrainViewModel> FilteredTrains
    {
        get
        {
            if (SelectedProject == null)
            {
                return [];
            }

            return string.IsNullOrWhiteSpace(TrainSearchText)
                ? [.. SelectedProject.Trains]
                : [.. SelectedProject.Trains.Where(train => train.Name.Contains(TrainSearchText, StringComparison.OrdinalIgnoreCase))];
        }
    }

    [RelayCommand]
    private void AddTrain()
    {
        if (SelectedProject == null)
        {
            return;
        }

        var train = EntityEditorHelper.AddEntity(
            SelectedProject.Model.Trains,
            SelectedProject.Trains,
            () => new Train { Name = $"Train {SelectedProject.Model.Trains.Count + 1}" },
            model => new TrainViewModel(model, SelectedProject));

        SelectedTrain = train;
        OnPropertyChanged(nameof(FilteredTrains));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTrain))]
    private void DeleteTrain()
    {
        if (SelectedProject == null)
        {
            return;
        }

        EntityEditorHelper.DeleteEntity(
            SelectedTrain,
            SelectedProject.Model.Trains,
            SelectedProject.Trains,
            () => SelectedTrain = SelectedProject.Trains.FirstOrDefault());

        OnPropertyChanged(nameof(FilteredTrains));
        OnPropertyChanged(nameof(SelectedVehicles));
    }

    private bool CanDeleteTrain() => SelectedTrain != null;

    public void AddLocomotiveToSelectedTrain(LocomotiveViewModel? locomotive, int insertIndex = -1)
    {
        AddVehicleToSelectedTrain(locomotive, insertIndex, static (train, value, index) => train.InsertLocomotive(value, index));
    }

    public void AddPassengerWagonToSelectedTrain(PassengerWagonViewModel? wagon, int insertIndex = -1)
    {
        AddVehicleToSelectedTrain(wagon, insertIndex, static (train, value, index) => train.InsertPassengerWagon(value, index));
    }

    public void AddGoodsWagonToSelectedTrain(GoodsWagonViewModel? wagon, int insertIndex = -1)
    {
        AddVehicleToSelectedTrain(wagon, insertIndex, static (train, value, index) => train.InsertGoodsWagon(value, index));
    }

    public void RemoveSelectedVehicle(VehicleItemViewModel? item)
    {
        if (item?.RemoveCommand == null)
        {
            return;
        }

        item.RemoveCommand.Execute(null);
        OnPropertyChanged(nameof(SelectedVehicles));
        SaveChanges();
    }

    public void SynchronizeSelectedVehicles()
    {
        if (SelectedTrain == null)
        {
            return;
        }

        SelectedTrain.SynchronizeVehiclesFromItems();
        OnPropertyChanged(nameof(SelectedVehicles));
        SaveChanges();
    }

    private void AddVehicleToSelectedTrain<TVehicle>(
        TVehicle? vehicle,
        int insertIndex,
        Action<TrainViewModel, TVehicle, int> insertAction)
        where TVehicle : class
    {
        if (SelectedTrain == null || vehicle == null)
        {
            return;
        }

        insertAction(SelectedTrain, vehicle, insertIndex);
        OnPropertyChanged(nameof(SelectedVehicles));
        SaveChanges();
    }
}
