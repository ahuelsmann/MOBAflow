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
using Service;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>Owns the locomotive editor shared by the locomotive page and train library.</summary>
public sealed partial class LocomotivesPageViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly PhotoAssignmentService _photos;
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<LocomotivesPageViewModel> _logger;
    private readonly Action<string> _showPhotoInExplorer;
    private ProjectViewModel? _observedProject;
    private List<LocomotiveViewModel> _filteredLocomotiveLibrary = [];
    private bool _isLocomotivesListExpanded;
    private bool _isLocomotivesPropertiesExpanded;
    private bool _disposed;

    public LocomotivesPageViewModel(
        ISolutionSession session,
        PhotoAssignmentService photos,
        ISettingsService settingsService,
        LayoutColumnWidthsViewModel layoutColumnWidths,
        LocomotiveManagementViewModel management,
        ILogger<LocomotivesPageViewModel> logger,
        Action<string> showPhotoInExplorer)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(photos);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(layoutColumnWidths);
        ArgumentNullException.ThrowIfNull(management);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(showPhotoInExplorer);
        _session = session;
        _photos = photos;
        _settings = settingsService.GetSettings();
        _settingsService = settingsService;
        _logger = logger;
        _showPhotoInExplorer = showPhotoInExplorer;
        LayoutColumnWidths = layoutColumnWidths;
        Management = management;
        _isLocomotivesListExpanded = _settings.Layout.LocomotivesPage.IsListExpanded;
        _isLocomotivesPropertiesExpanded = _settings.Layout.LocomotivesPage.IsPropertiesExpanded;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.SolutionReplacing += OnSolutionReplacing;
        ObserveProject(_session.SelectedProject);
    }

    public LayoutColumnWidthsViewModel LayoutColumnWidths { get; }

    public LocomotiveManagementViewModel Management { get; }

    public ProjectViewModel? SelectedProject => _session.SelectedProject;

    [ObservableProperty]
    public partial LocomotiveViewModel? SelectedLocomotive { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLocomotiveLibrary))]
    public partial string LocomotiveSearchText { get; set; } = string.Empty;

    // Keep ItemsSource identity on a rename so WinUI does not clear the two-way selection.
    public IReadOnlyList<LocomotiveViewModel> FilteredLocomotiveLibrary
    {
        get
        {
            var search = LocomotiveSearchText.Trim();
            var matches = SelectedProject?.Locomotives
                .Where(locomotive => locomotive.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
            if (!_filteredLocomotiveLibrary.SequenceEqual(matches))
                _filteredLocomotiveLibrary = matches;
            return _filteredLocomotiveLibrary;
        }
    }

    public bool IsLocomotivesListExpanded
    {
        get => _isLocomotivesListExpanded;
        set
        {
            if (SetProperty(ref _isLocomotivesListExpanded, value))
            {
                _settings.Layout.LocomotivesPage.IsListExpanded = value;
                PersistSettings();
            }
        }
    }

    public bool IsLocomotivesPropertiesExpanded
    {
        get => _isLocomotivesPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isLocomotivesPropertiesExpanded, value))
            {
                _settings.Layout.LocomotivesPage.IsPropertiesExpanded = value;
                PersistSettings();
            }
        }
    }

    private void PersistSettings() => _settingsService.SaveSettingsAsync(_settings)
        .Observe(ex => LogSettingsSaveFailed(_logger, ex));

    private void SaveChanges(string operation) => _session.SaveSolutionInternalAsync()
        .Observe(ex => LogSaveFailed(_logger, operation, ex));

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? oldValue, LocomotiveViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnLocomotivePropertyChanged;
            _session.UntrackChanges(oldValue);
        }
        if (newValue is not null && !_disposed)
        {
            AttachLocomotivePhotoCommand(newValue);
            newValue.PropertyChanged += OnLocomotivePropertyChanged;
            _session.TrackChanges(newValue);
        }
        DeleteLocomotiveCommand.NotifyCanExecuteChanged();
        RefreshManagement();
    }

    partial void OnLocomotiveSearchTextChanged(string value)
    {
        _ = value;
        RefreshManagement();
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISolutionSession.SelectedProject))
        {
            ObserveProject(_session.SelectedProject);
            OnPropertyChanged(nameof(SelectedProject));
        }
    }

    private void OnSolutionReplacing(object? sender, EventArgs e) => SelectedLocomotive = null;

    private void ObserveProject(ProjectViewModel? project)
    {
        if (_observedProject is not null)
            _observedProject.Locomotives.CollectionChanged -= OnLocomotivesChanged;
        SelectedLocomotive = null;
        _observedProject = project;
        if (project is not null)
            project.Locomotives.CollectionChanged += OnLocomotivesChanged;
        OnPropertyChanged(nameof(FilteredLocomotiveLibrary));
        RefreshManagement();
    }

    private void OnLocomotivesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectedLocomotive is { } locomotive && !(_observedProject?.Locomotives.Contains(locomotive) ?? false))
            SelectedLocomotive = null;
        OnPropertyChanged(nameof(FilteredLocomotiveLibrary));
    }

    private void OnLocomotivePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocomotiveViewModel.Name))
            OnPropertyChanged(nameof(FilteredLocomotiveLibrary));
        if (e.PropertyName is nameof(LocomotiveViewModel.Name) or nameof(LocomotiveViewModel.DigitalAddress))
            RefreshManagement();
    }

    private void RefreshManagement() => Management.SetContext(_disposed ? null : SelectedProject?.Model, SelectedLocomotive?.Model);

    public PhotoAssignmentTarget AssignUploadedPhoto(string photoPath)
    {
        if (_disposed || string.IsNullOrWhiteSpace(photoPath) || SelectedLocomotive is null)
            return PhotoAssignmentTarget.None;
        SelectedLocomotive.PhotoPath = photoPath;
        return PhotoAssignmentTarget.Locomotive;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _session.SolutionReplacing -= OnSolutionReplacing;
        // Shutdown may continue off the UI thread: release subscriptions without changing bound state.
        if (_observedProject is not null)
            _observedProject.Locomotives.CollectionChanged -= OnLocomotivesChanged;
        _observedProject = null;
        if (SelectedLocomotive is { } locomotive)
        {
            locomotive.PropertyChanged -= OnLocomotivePropertyChanged;
            _session.UntrackChanges(locomotive);
        }
        GC.SuppressFinalize(this);
    }

    private void AttachLocomotivePhotoCommand(LocomotiveViewModel? locomotive)
    {
        if (locomotive is null || locomotive.BrowsePhotoCommand is not null) return;
        locomotive.BrowsePhotoCommand = new AsyncRelayCommand(() => BrowseLocomotivePhotoAsync(locomotive));
        locomotive.DeletePhotoCommand = new RelayCommand(() => DeleteLocomotivePhoto(locomotive));
        locomotive.ShowInExplorerCommand = new RelayCommand(() => ShowLocomotivePhotoInExplorer(locomotive));
    }

    private async Task BrowseLocomotivePhotoAsync(LocomotiveViewModel locomotive)
    {
        var solution = _session.Solution;
        var project = SelectedProject?.Model;
        var photoPath = await _photos.PickPhotoAsync().ConfigureAwait(true);
        if (string.IsNullOrEmpty(photoPath)) return;
        if (_disposed || project is null || !ReferenceEquals(_session.Solution, solution) || !solution.Projects.Contains(project)) return;

        var saved = await _photos.StorePhotoAsync(photoPath, "locomotives", locomotive.Model.Id).ConfigureAwait(true);
        if (_disposed || saved is null || !ReferenceEquals(_session.Solution, solution) || !solution.Projects.Contains(project)) return;

        if (locomotive.PhotoPath == saved)
            locomotive.InvalidatePhotoBinding();
        else
            locomotive.PhotoPath = saved;

        SaveChanges("Save vehicle photo");
        LogPhotoSaved(_logger, locomotive.Name);
    }

    private void DeleteLocomotivePhoto(LocomotiveViewModel locomotive)
    {
        locomotive.PhotoPath = null;
        LogPhotoDeleted(_logger, locomotive.Name);
    }

    private void ShowLocomotivePhotoInExplorer(LocomotiveViewModel locomotive)
    {
        if (string.IsNullOrWhiteSpace(locomotive.PhotoPath)) return;
        var fullPath = _photos.GetPhotoFullPath(locomotive.PhotoPath);
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

    #region Locomotive Commands
    [RelayCommand]
    private void AddLocomotive()
    {
        if (SelectedProject?.Model == null)
        {
            LogNoProject(_logger);
            return;
        }

        var locomotive = new Locomotive
        {
            Name = $"Locomotive {SelectedProject.Model.Locomotives.Count + 1}"
        };

        SelectedProject.Model.Locomotives.Add(locomotive);

        var locoVm = new LocomotiveViewModel(locomotive);
        SelectedProject.Locomotives.Add(locoVm);
        SelectedLocomotive = locoVm;
        AttachLocomotivePhotoCommand(SelectedLocomotive);

        LogLocomotiveAdded(_logger, locomotive.Name);
        SaveChanges("Save rolling stock");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLocomotive))]
    private void DeleteLocomotive()
    {
        // Snapshot: Copy references before collection changes trigger PropertyChanged
        var selectedLoco = SelectedLocomotive;
        var selectedProject = SelectedProject;

        if (selectedLoco?.Model == null || selectedProject?.Model == null)
        {
            return;
        }

        var locomotiveName = selectedLoco.Name;
        var locoModel = selectedLoco.Model;

        // Remove from ViewModel collection first (may trigger selection change)
        selectedProject.Locomotives.Remove(selectedLoco);

        // Remove from Domain model
        selectedProject.Model.Locomotives.Remove(locoModel);

        // Clear selection only if it's still the same object
        if (ReferenceEquals(SelectedLocomotive, selectedLoco))
        {
            SelectedLocomotive = null;
        }

        LogLocomotiveDeleted(_logger, locomotiveName);
        SaveChanges("Save rolling stock");
    }

    private bool CanDeleteLocomotive() => SelectedLocomotive != null;
    #endregion

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot add locomotive: No project selected")]
    private static partial void LogNoProject(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Added new locomotive: {Name}")]
    private static partial void LogLocomotiveAdded(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted locomotive: {Name}")]
    private static partial void LogLocomotiveDeleted(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Photo saved for locomotive: {Name}")]
    private static partial void LogPhotoSaved(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Photo deleted for locomotive: {Name}")]
    private static partial void LogPhotoDeleted(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Photo file not found: {Path}")]
    private static partial void LogPhotoMissing(ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Opened Explorer at: {Path}")]
    private static partial void LogExplorerOpened(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to open Explorer for: {Path}")]
    private static partial void LogExplorerFailed(ILogger logger, string path, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogSaveFailed(ILogger logger, string operationName, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Persist locomotive settings failed")]
    private static partial void LogSettingsSaveFailed(ILogger logger, Exception exception);
}
