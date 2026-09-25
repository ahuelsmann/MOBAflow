// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>
/// MainWindowViewModel - Locomotive/Wagon Management Partial.
/// </summary>
public partial class MainWindowViewModel
{
    private const string LocomotivesPageTag = "locomotives";
    private const string PassengerWagonsPageTag = "passengerwagons";
    private const string GoodsWagonsPageTag = "goodswagons";
    private const string SaveRollingStockOperation = "Save rolling stock";

    private string? _activePhotoAssignmentPageTag;

    #region Locomotive/Wagon Selection Properties
    [ObservableProperty]
    private LocomotiveViewModel? _selectedLocomotive;

    [ObservableProperty]
    private PassengerWagonViewModel? _selectedPassengerWagon;

    [ObservableProperty]
    private GoodsWagonViewModel? _selectedGoodsWagon;

    /// <summary>
    /// Search text for filtering locomotives.
    /// </summary>
    [ObservableProperty]
    private string _locomotiveSearchText = string.Empty;

    /// <summary>
    /// Search text for filtering passenger wagons.
    /// </summary>
    [ObservableProperty]
    private string _passengerWagonSearchText = string.Empty;

    /// <summary>
    /// Search text for filtering goods wagons.
    /// </summary>
    [ObservableProperty]
    private string _goodsWagonSearchText = string.Empty;

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? oldValue, LocomotiveViewModel? newValue)
    {
        AttachLocomotivePhotoCommand(newValue);
        DeleteLocomotiveCommand.NotifyCanExecuteChanged();

        if (oldValue != null)
            oldValue.PropertyChanged -= OnRollingStockPropertyChanged;

        // Subscribe to PropertyChanged for auto-save
        if (newValue != null)
        {
            newValue.PropertyChanged += OnRollingStockPropertyChanged;
        }
    }

    partial void OnSelectedPassengerWagonChanged(PassengerWagonViewModel? oldValue, PassengerWagonViewModel? newValue)
    {
        AttachWagonPhotoCommand(newValue);
        DeletePassengerWagonCommand.NotifyCanExecuteChanged();

        if (oldValue != null)
            oldValue.PropertyChanged -= OnRollingStockPropertyChanged;

        // Subscribe to PropertyChanged for auto-save
        if (newValue != null)
        {
            newValue.PropertyChanged += OnRollingStockPropertyChanged;
        }
    }

    partial void OnSelectedGoodsWagonChanged(GoodsWagonViewModel? oldValue, GoodsWagonViewModel? newValue)
    {
        AttachWagonPhotoCommand(newValue);
        DeleteGoodsWagonCommand.NotifyCanExecuteChanged();

        if (oldValue != null)
            oldValue.PropertyChanged -= OnRollingStockPropertyChanged;

        // Subscribe to PropertyChanged for auto-save
        if (newValue != null)
        {
            newValue.PropertyChanged += OnRollingStockPropertyChanged;
        }
    }

    private void ObserveRollingStockProject(ProjectViewModel? oldProject, ProjectViewModel? project)
    {
        if (oldProject is not null)
        {
            oldProject.PropertyChanged -= OnViewModelPropertyChanged;
            oldProject.Locomotives.CollectionChanged -= OnRollingStockCollectionChanged;
            oldProject.PassengerWagons.CollectionChanged -= OnRollingStockCollectionChanged;
            oldProject.GoodsWagons.CollectionChanged -= OnRollingStockCollectionChanged;
        }

        SelectedLocomotive = null;
        SelectedPassengerWagon = null;
        SelectedGoodsWagon = null;

        if (project is not null)
        {
            project.Locomotives.CollectionChanged += OnRollingStockCollectionChanged;
            project.PassengerWagons.CollectionChanged += OnRollingStockCollectionChanged;
            project.GoodsWagons.CollectionChanged += OnRollingStockCollectionChanged;
        }

        NotifyRollingStockLibrariesChanged();
    }

    private void OnRollingStockCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var project = SelectedProject;
        if (SelectedLocomotive is { } locomotive && (project is null || !project.Locomotives.Contains(locomotive)))
            SelectedLocomotive = null;
        if (SelectedPassengerWagon is { } passengerWagon && (project is null || !project.PassengerWagons.Contains(passengerWagon)))
            SelectedPassengerWagon = null;
        if (SelectedGoodsWagon is { } goodsWagon && (project is null || !project.GoodsWagons.Contains(goodsWagon)))
            SelectedGoodsWagon = null;

        NotifyRollingStockLibrariesChanged();
    }

    private void OnRollingStockPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocomotiveViewModel.Name))
            NotifyRollingStockLibrariesChanged();

        OnViewModelPropertyChanged(sender, e);
    }

    private void NotifyRollingStockLibrariesChanged()
    {
        OnPropertyChanged(nameof(FilteredLocomotiveLibrary));
        OnPropertyChanged(nameof(FilteredPassengerWagonLibrary));
        OnPropertyChanged(nameof(FilteredGoodsWagonLibrary));
    }

    private void AttachWagonPhotoCommand(WagonViewModel? wagonVm)
    {
        if (wagonVm == null) return;
        if (wagonVm.BrowsePhotoCommand != null) return;

        wagonVm.BrowsePhotoCommand = new AsyncRelayCommand(async () =>
        {
            var solution = Solution;
            var project = SelectedProject?.Model;
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;
            if (project is null || !ReferenceEquals(Solution, solution) || !solution.Projects.Contains(project)) return;

            var saved = await _ioService.SavePhotoAsync(photoPath, "wagons", wagonVm.Model.Id);
            if (saved != null && ReferenceEquals(Solution, solution) && solution.Projects.Contains(project))
            {
                if (wagonVm.PhotoPath == saved)
                {
                    wagonVm.InvalidatePhotoBinding();
                }
                else
                {
                    wagonVm.PhotoPath = saved;
                }

                ObserveBackgroundTask(SaveSolutionInternalAsync(), "Save vehicle photo");
                _logger.LogInformation("Photo saved for wagon: {Name}", wagonVm.Name);
            }
        });

        wagonVm.DeletePhotoCommand = new RelayCommand(() =>
        {
            wagonVm.PhotoPath = null;
            _logger.LogInformation("Photo deleted for wagon: {Name}", wagonVm.Name);
        });

        wagonVm.ShowInExplorerCommand = new RelayCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(wagonVm.PhotoPath)) return;

            var fullPath = _ioService.GetPhotoFullPath(wagonVm.PhotoPath);
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                _logger.LogWarning("Photo file not found: {Path}", fullPath);
                return;
            }

            try
            {
                Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                _logger.LogInformation("Opened Explorer at: {Path}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open Explorer for: {Path}", fullPath);
            }
        });
    }

    private void AttachLocomotivePhotoCommand(LocomotiveViewModel? locoVm)
    {
        if (locoVm == null) return;
        if (locoVm.BrowsePhotoCommand != null) return;

        locoVm.BrowsePhotoCommand = new AsyncRelayCommand(async () =>
        {
            var solution = Solution;
            var project = SelectedProject?.Model;
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;
            if (project is null || !ReferenceEquals(Solution, solution) || !solution.Projects.Contains(project)) return;

            var saved = await _ioService.SavePhotoAsync(photoPath, "locomotives", locoVm.Model.Id);
            if (saved != null && ReferenceEquals(Solution, solution) && solution.Projects.Contains(project))
            {
                if (locoVm.PhotoPath == saved)
                {
                    locoVm.InvalidatePhotoBinding();
                }
                else
                {
                    locoVm.PhotoPath = saved;
                }

                ObserveBackgroundTask(SaveSolutionInternalAsync(), "Save vehicle photo");
                _logger.LogInformation("Photo saved for locomotive: {Name}", locoVm.Name);
            }
        });

        locoVm.DeletePhotoCommand = new RelayCommand(() =>
        {
            locoVm.PhotoPath = null;
            _logger.LogInformation("Photo deleted for locomotive: {Name}", locoVm.Name);
        });

        locoVm.ShowInExplorerCommand = new RelayCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(locoVm.PhotoPath)) return;

            var fullPath = _ioService.GetPhotoFullPath(locoVm.PhotoPath);
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                _logger.LogWarning("Photo file not found: {Path}", fullPath);
                return;
            }

            try
            {
                Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                _logger.LogInformation("Opened Explorer at: {Path}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open Explorer for: {Path}", fullPath);
            }
        });
    }

    /// <summary>
    /// Updates the active page tag used to resolve uploaded photo assignment target.
    /// </summary>
    public void UpdateActivePhotoAssignmentPageTag(string? pageTag)
    {
        _activePhotoAssignmentPageTag = pageTag;
    }

    /// <summary>
    /// Assigns an uploaded photo to the currently selected entity.
    /// Assignment is page-aware and only uses the selection of the active vehicle page.
    /// </summary>
    public PhotoAssignmentTarget AssignUploadedPhotoToSelectedEntity(string photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return PhotoAssignmentTarget.None;
        }

        return _activePhotoAssignmentPageTag switch
        {
            LocomotivesPageTag when SelectedLocomotive != null => AssignPhotoToSelectedLocomotive(photoPath),
            PassengerWagonsPageTag when SelectedPassengerWagon != null => AssignPhotoToSelectedPassengerWagon(photoPath),
            GoodsWagonsPageTag when SelectedGoodsWagon != null => AssignPhotoToSelectedGoodsWagon(photoPath),
            _ => PhotoAssignmentTarget.None,
        };
    }

    private PhotoAssignmentTarget AssignPhotoToSelectedLocomotive(string photoPath)
    {
        SelectedLocomotive!.PhotoPath = photoPath;
        return PhotoAssignmentTarget.Locomotive;
    }

    private PhotoAssignmentTarget AssignPhotoToSelectedPassengerWagon(string photoPath)
    {
        SelectedPassengerWagon!.PhotoPath = photoPath;
        return PhotoAssignmentTarget.PassengerWagon;
    }

    private PhotoAssignmentTarget AssignPhotoToSelectedGoodsWagon(string photoPath)
    {
        SelectedGoodsWagon!.PhotoPath = photoPath;
        return PhotoAssignmentTarget.GoodsWagon;
    }
    #endregion

    #region Locomotive Commands
    [RelayCommand]
    private void AddLocomotive()
    {
        if (SelectedProject?.Model == null)
        {
            _logger.LogWarning("Cannot add locomotive: No project selected");
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

        _logger.LogInformation("Added new locomotive: {Name}", locomotive.Name);
        ObserveBackgroundTask(SaveSolutionInternalAsync(), SaveRollingStockOperation);
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

        _logger.LogInformation("Deleted locomotive: {Name}", locomotiveName);
        ObserveBackgroundTask(SaveSolutionInternalAsync(), SaveRollingStockOperation);
    }

    private bool CanDeleteLocomotive() => SelectedLocomotive != null;
    #endregion

    #region Passenger Wagon Commands
    [RelayCommand]
    private void AddPassengerWagon()
    {
        if (SelectedProject?.Model == null)
        {
            _logger.LogWarning("Cannot add passenger wagon: No project selected");
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

        _logger.LogInformation("Added new passenger wagon: {Name}", wagon.Name);
        ObserveBackgroundTask(SaveSolutionInternalAsync(), SaveRollingStockOperation);
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

        _logger.LogInformation("Deleted passenger wagon: {Name}", wagonName);
        ObserveBackgroundTask(SaveSolutionInternalAsync(), SaveRollingStockOperation);
    }

    private bool CanDeletePassengerWagon() => SelectedPassengerWagon != null;
    #endregion

    #region Goods Wagon Commands
    [RelayCommand]
    private void AddGoodsWagon()
    {
        if (SelectedProject?.Model == null)
        {
            _logger.LogWarning("Cannot add goods wagon: No project selected");
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

        _logger.LogInformation("Added new goods wagon: {Name}", wagon.Name);
        ObserveBackgroundTask(SaveSolutionInternalAsync(), SaveRollingStockOperation);
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

        _logger.LogInformation("Deleted goods wagon: {Name}", wagonName);
        ObserveBackgroundTask(SaveSolutionInternalAsync(), SaveRollingStockOperation);
    }

    private bool CanDeleteGoodsWagon() => SelectedGoodsWagon != null;
    #endregion
}