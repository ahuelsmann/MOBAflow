// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using Common.Events;
using Common.Extension;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Interface;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>Owns passenger and goods wagon editing shared by inventory pages and train libraries.</summary>
public sealed partial class WagonsPageViewModel : ObservableObject, IDisposable
{
    private const string SaveRollingStockOperation = "Save rolling stock";

    private readonly ISolutionSession _session;
    private readonly IIoService _ioService;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<WagonsPageViewModel> _logger;
    private readonly Action<string> _showPhotoInExplorer;
    private ProjectViewModel? _observedProject;
    private List<PassengerWagonViewModel> _filteredPassengerWagonLibrary = [];
    private List<GoodsWagonViewModel> _filteredGoodsWagonLibrary = [];
    private bool _isPassengerWagonListExpanded;
    private bool _isPassengerWagonPropertiesExpanded;
    private bool _isGoodsWagonListExpanded;
    private bool _isGoodsWagonPropertiesExpanded;
    private bool _disposed;

    public WagonsPageViewModel(
        ISolutionSession session,
        IIoService ioService,
        ISettingsService settingsService,
        LayoutColumnWidthsViewModel layoutColumnWidths,
        ILogger<WagonsPageViewModel> logger,
        Action<string> showPhotoInExplorer)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(ioService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(showPhotoInExplorer);
        _session = session;
        _ioService = ioService;
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();
        _logger = logger;
        _showPhotoInExplorer = showPhotoInExplorer;
        LayoutColumnWidths = layoutColumnWidths;
        _isPassengerWagonListExpanded = _settings.Layout.PassengerWagonPage.IsListExpanded;
        _isPassengerWagonPropertiesExpanded = _settings.Layout.PassengerWagonPage.IsPropertiesExpanded;
        _isGoodsWagonListExpanded = _settings.Layout.GoodsWagonPage.IsListExpanded;
        _isGoodsWagonPropertiesExpanded = _settings.Layout.GoodsWagonPage.IsPropertiesExpanded;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.SolutionReplacing += OnSolutionReplacing;
        ObserveProject(_session.SelectedProject);
    }

    public LayoutColumnWidthsViewModel LayoutColumnWidths { get; }

    public ProjectViewModel? SelectedProject => _session.SelectedProject;

    [ObservableProperty]
    public partial PassengerWagonViewModel? SelectedPassengerWagon { get; set; }

    [ObservableProperty]
    public partial GoodsWagonViewModel? SelectedGoodsWagon { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPassengerWagonLibrary))]
    public partial string PassengerWagonSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGoodsWagonLibrary))]
    public partial string GoodsWagonSearchText { get; set; } = string.Empty;

    // Preserve ItemsSource identity on rename so WinUI keeps the two-way selection.
    public IReadOnlyList<PassengerWagonViewModel> FilteredPassengerWagonLibrary
    {
        get
        {
            var search = PassengerWagonSearchText.Trim();
            var matches = SelectedProject?.PassengerWagons
                .Where(wagon => wagon.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
            if (!_filteredPassengerWagonLibrary.SequenceEqual(matches))
                _filteredPassengerWagonLibrary = matches;
            return _filteredPassengerWagonLibrary;
        }
    }

    public IReadOnlyList<GoodsWagonViewModel> FilteredGoodsWagonLibrary
    {
        get
        {
            var search = GoodsWagonSearchText.Trim();
            var matches = SelectedProject?.GoodsWagons
                .Where(wagon => wagon.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
            if (!_filteredGoodsWagonLibrary.SequenceEqual(matches))
                _filteredGoodsWagonLibrary = matches;
            return _filteredGoodsWagonLibrary;
        }
    }

    public bool IsPassengerWagonListExpanded
    {
        get => _isPassengerWagonListExpanded;
        set
        {
            if (SetProperty(ref _isPassengerWagonListExpanded, value))
            {
                _settings.Layout.PassengerWagonPage.IsListExpanded = value;
                PersistSettings();
            }
        }
    }

    public bool IsPassengerWagonPropertiesExpanded
    {
        get => _isPassengerWagonPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isPassengerWagonPropertiesExpanded, value))
            {
                _settings.Layout.PassengerWagonPage.IsPropertiesExpanded = value;
                PersistSettings();
            }
        }
    }

    public bool IsGoodsWagonListExpanded
    {
        get => _isGoodsWagonListExpanded;
        set
        {
            if (SetProperty(ref _isGoodsWagonListExpanded, value))
            {
                _settings.Layout.GoodsWagonPage.IsListExpanded = value;
                PersistSettings();
            }
        }
    }

    public bool IsGoodsWagonPropertiesExpanded
    {
        get => _isGoodsWagonPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isGoodsWagonPropertiesExpanded, value))
            {
                _settings.Layout.GoodsWagonPage.IsPropertiesExpanded = value;
                PersistSettings();
            }
        }
    }

    private void PersistSettings() => _settingsService.SaveSettingsAsync(_settings)
        .Observe(ex => LogSettingsSaveFailed(_logger, ex));

    private void SaveChanges(string operation) => _session.SaveSolutionInternalAsync()
        .Observe(ex => LogSaveFailed(_logger, operation, ex));

    partial void OnSelectedPassengerWagonChanged(PassengerWagonViewModel? oldValue, PassengerWagonViewModel? newValue)
    {
        ObserveWagon(oldValue, newValue);
        DeletePassengerWagonCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedGoodsWagonChanged(GoodsWagonViewModel? oldValue, GoodsWagonViewModel? newValue)
    {
        ObserveWagon(oldValue, newValue);
        DeleteGoodsWagonCommand.NotifyCanExecuteChanged();
    }

    private void ObserveWagon(WagonViewModel? oldValue, WagonViewModel? newValue)
    {
        DetachWagon(oldValue);
        if (newValue is not null && !_disposed)
        {
            AttachWagonPhotoCommand(newValue);
            newValue.PropertyChanged += OnWagonPropertyChanged;
            _session.TrackChanges(newValue);
        }
    }

    private void DetachWagon(WagonViewModel? wagon)
    {
        if (wagon is null) return;
        wagon.PropertyChanged -= OnWagonPropertyChanged;
        _session.UntrackChanges(wagon);
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            ObserveProject(_session.SelectedProject);
            OnPropertyChanged(nameof(SelectedProject));
        }
    }

    private void OnSolutionReplacing(object? sender, EventArgs e)
    {
        SelectedPassengerWagon = null;
        SelectedGoodsWagon = null;
    }

    private void DetachProject()
    {
        if (_observedProject is null) return;
        _observedProject.PassengerWagons.CollectionChanged -= OnWagonsChanged;
        _observedProject.GoodsWagons.CollectionChanged -= OnWagonsChanged;
    }

    private void ObserveProject(ProjectViewModel? project)
    {
        DetachProject();
        SelectedPassengerWagon = null;
        SelectedGoodsWagon = null;
        _observedProject = project;
        if (project is not null)
        {
            project.PassengerWagons.CollectionChanged += OnWagonsChanged;
            project.GoodsWagons.CollectionChanged += OnWagonsChanged;
        }
        NotifyLibrariesChanged();
    }

    private void OnWagonsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedPassengerWagon is { } passenger && !(_observedProject?.PassengerWagons.Contains(passenger) ?? false))
            SelectedPassengerWagon = null;
        if (SelectedGoodsWagon is { } goods && !(_observedProject?.GoodsWagons.Contains(goods) ?? false))
            SelectedGoodsWagon = null;
        NotifyLibrariesChanged();
    }

    private void OnWagonPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WagonViewModel.Name))
            NotifyLibrariesChanged();
    }

    private void NotifyLibrariesChanged()
    {
        OnPropertyChanged(nameof(FilteredPassengerWagonLibrary));
        OnPropertyChanged(nameof(FilteredGoodsWagonLibrary));
    }

    public PhotoAssignmentTarget AssignUploadedPassengerWagonPhoto(string photoPath)
    {
        if (_disposed || SelectedPassengerWagon is null) return PhotoAssignmentTarget.None;
        SelectedPassengerWagon.PhotoPath = photoPath;
        return PhotoAssignmentTarget.PassengerWagon;
    }

    public PhotoAssignmentTarget AssignUploadedGoodsWagonPhoto(string photoPath)
    {
        if (_disposed || SelectedGoodsWagon is null) return PhotoAssignmentTarget.None;
        SelectedGoodsWagon.PhotoPath = photoPath;
        return PhotoAssignmentTarget.GoodsWagon;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _session.SolutionReplacing -= OnSolutionReplacing;
        // Shutdown may continue off the UI thread: detach without changing bound selection.
        DetachProject();
        _observedProject = null;
        DetachWagon(SelectedPassengerWagon);
        DetachWagon(SelectedGoodsWagon);
        GC.SuppressFinalize(this);
    }

    private void AttachWagonPhotoCommand(WagonViewModel? wagon)
    {
        if (wagon is null || wagon.BrowsePhotoCommand is not null) return;
        wagon.BrowsePhotoCommand = new AsyncRelayCommand(() => BrowseWagonPhotoAsync(wagon));
        wagon.DeletePhotoCommand = new RelayCommand(() => DeleteWagonPhoto(wagon));
        wagon.ShowInExplorerCommand = new RelayCommand(() => ShowWagonPhotoInExplorer(wagon));
    }

    private async Task BrowseWagonPhotoAsync(WagonViewModel wagon)
    {
        var solution = _session.Solution;
        var project = SelectedProject?.Model;
        var photoPath = await _ioService.BrowseForPhotoAsync().ConfigureAwait(true);
        if (string.IsNullOrEmpty(photoPath)) return;
        if (_disposed || project is null || !ReferenceEquals(_session.Solution, solution) || !solution.Projects.Contains(project)) return;

        var saved = await _ioService.SavePhotoAsync(photoPath, "wagons", wagon.Model.Id).ConfigureAwait(true);
        if (_disposed || saved is null || !ReferenceEquals(_session.Solution, solution) || !solution.Projects.Contains(project)) return;

        if (wagon.PhotoPath == saved)
            wagon.InvalidatePhotoBinding();
        else
            wagon.PhotoPath = saved;
        SaveChanges("Save vehicle photo");
        LogPhotoSaved(_logger, wagon.Name);
    }

    private void DeleteWagonPhoto(WagonViewModel wagon)
    {
        wagon.PhotoPath = null;
        LogPhotoDeleted(_logger, wagon.Name);
    }

    private void ShowWagonPhotoInExplorer(WagonViewModel wagon)
    {
        if (string.IsNullOrWhiteSpace(wagon.PhotoPath)) return;
        var fullPath = _ioService.GetPhotoFullPath(wagon.PhotoPath);
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            LogPhotoMissing(_logger, fullPath);
            return;
        }
        try
        {
            _showPhotoInExplorer(fullPath);
            LogExplorerOpened(_logger, fullPath);
        }
        catch (Exception ex)
        {
            LogExplorerFailed(_logger, fullPath, ex);
        }
    }
    #region Passenger Wagon Commands
    [RelayCommand]
    private void AddPassengerWagon()
    {
        if (SelectedProject?.Model == null)
        {
            LogNoProject(_logger, "passenger");
            return;
        }

        var wagon = new PassengerWagon
        {
            Name = $"Passenger Wagon {SelectedProject.Model.PassengerWagons.Count + 1}"
        };

        SelectedProject.Model.PassengerWagons.Add(wagon);
        var wagonVm = new PassengerWagonViewModel(wagon);
        SelectedProject.PassengerWagons.Add(wagonVm);
        SelectedPassengerWagon = wagonVm;
        AttachWagonPhotoCommand(SelectedPassengerWagon);

        LogWagonAdded(_logger, "passenger", wagon.Name);
        SaveChanges(SaveRollingStockOperation);
    }

    [RelayCommand(CanExecute = nameof(CanDeletePassengerWagon))]
    private void DeletePassengerWagon()
    {
        if (SelectedPassengerWagon?.Model == null || SelectedProject?.Model == null)
            return;

        var selectedWagon = SelectedPassengerWagon;
        var project = SelectedProject;
        var wagonName = selectedWagon.Name;
        project.PassengerWagons.Remove(selectedWagon);
        project.Model.PassengerWagons.Remove((PassengerWagon)selectedWagon.Model);
        if (ReferenceEquals(SelectedPassengerWagon, selectedWagon))
            SelectedPassengerWagon = null;

        LogWagonDeleted(_logger, "passenger", wagonName);
        SaveChanges(SaveRollingStockOperation);
    }

    private bool CanDeletePassengerWagon() => SelectedPassengerWagon != null;
    #endregion

    #region Goods Wagon Commands
    [RelayCommand]
    private void AddGoodsWagon()
    {
        if (SelectedProject?.Model == null)
        {
            LogNoProject(_logger, "goods");
            return;
        }

        var wagon = new GoodsWagon
        {
            Name = $"Goods Wagon {SelectedProject.Model.GoodsWagons.Count + 1}"
        };

        SelectedProject.Model.GoodsWagons.Add(wagon);
        var wagonVm = new GoodsWagonViewModel(wagon);
        SelectedProject.GoodsWagons.Add(wagonVm);
        SelectedGoodsWagon = wagonVm;
        AttachWagonPhotoCommand(SelectedGoodsWagon);

        LogWagonAdded(_logger, "goods", wagon.Name);
        SaveChanges(SaveRollingStockOperation);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteGoodsWagon))]
    private void DeleteGoodsWagon()
    {
        if (SelectedGoodsWagon?.Model == null || SelectedProject?.Model == null)
            return;

        var selectedWagon = SelectedGoodsWagon;
        var project = SelectedProject;
        var wagonName = selectedWagon.Name;
        project.GoodsWagons.Remove(selectedWagon);
        project.Model.GoodsWagons.Remove((GoodsWagon)selectedWagon.Model);
        if (ReferenceEquals(SelectedGoodsWagon, selectedWagon))
            SelectedGoodsWagon = null;

        LogWagonDeleted(_logger, "goods", wagonName);
        SaveChanges(SaveRollingStockOperation);
    }

    private bool CanDeleteGoodsWagon() => SelectedGoodsWagon != null;
    #endregion
    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot add {Kind} wagon: No project selected")]
    private static partial void LogNoProject(ILogger logger, string kind);

    [LoggerMessage(Level = LogLevel.Information, Message = "Added new {Kind} wagon: {Name}")]
    private static partial void LogWagonAdded(ILogger logger, string kind, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Kind} wagon: {Name}")]
    private static partial void LogWagonDeleted(ILogger logger, string kind, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Photo saved for wagon: {Name}")]
    private static partial void LogPhotoSaved(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Photo deleted for wagon: {Name}")]
    private static partial void LogPhotoDeleted(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Photo file not found: {Path}")]
    private static partial void LogPhotoMissing(ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Opened Explorer at: {Path}")]
    private static partial void LogExplorerOpened(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to open Explorer for: {Path}")]
    private static partial void LogExplorerFailed(ILogger logger, string path, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogSaveFailed(ILogger logger, string operationName, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Persist wagon settings failed")]
    private static partial void LogSettingsSaveFailed(ILogger logger, Exception exception);
}
