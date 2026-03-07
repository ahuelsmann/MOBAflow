// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Common.Path;
using Domain;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

/// <summary>
/// MainWindowViewModel - Locomotive/Wagon Management Partial.
/// </summary>
public partial class MainWindowViewModel
{
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

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? value)
    {
        AttachLocomotivePhotoCommand(value);
        DeleteLocomotiveCommand.NotifyCanExecuteChanged();

        // Subscribe to PropertyChanged for auto-save
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    partial void OnSelectedPassengerWagonChanged(PassengerWagonViewModel? value)
    {
        AttachWagonPhotoCommand(value);
        DeletePassengerWagonCommand.NotifyCanExecuteChanged();

        // Subscribe to PropertyChanged for auto-save
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    partial void OnSelectedGoodsWagonChanged(GoodsWagonViewModel? value)
    {
        AttachWagonPhotoCommand(value);
        DeleteGoodsWagonCommand.NotifyCanExecuteChanged();

        // Subscribe to PropertyChanged for auto-save
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void AttachWagonPhotoCommand(WagonViewModel? wagonVm)
    {
        if (wagonVm == null) return;
        if (wagonVm.BrowsePhotoCommand != null) return;

        wagonVm.BrowsePhotoCommand = new AsyncRelayCommand(async () =>
        {
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;

            var saved = await _ioService.SavePhotoAsync(photoPath, "wagons", wagonVm.Model.Id);
            if (saved != null)
            {
                wagonVm.PhotoPath = saved;
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
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;

            var saved = await _ioService.SavePhotoAsync(photoPath, "locomotives", locoVm.Model.Id);
            if (saved != null)
            {
                locoVm.PhotoPath = saved;
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
    }

    [RelayCommand(CanExecute = nameof(CanDeletePassengerWagon))]
    private void DeletePassengerWagon()
    {
        if (SelectedPassengerWagon?.Model == null || SelectedProject?.Model == null)
            return;

        var wagonName = SelectedPassengerWagon.Name;
        SelectedProject.PassengerWagons.Remove(SelectedPassengerWagon);
        SelectedProject.Model.PassengerWagons.Remove((PassengerWagon)SelectedPassengerWagon.Model);
        SelectedPassengerWagon = null;

        _logger.LogInformation("Deleted passenger wagon: {Name}", wagonName);
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
    }

    [RelayCommand(CanExecute = nameof(CanDeleteGoodsWagon))]
    private void DeleteGoodsWagon()
    {
        if (SelectedGoodsWagon?.Model == null || SelectedProject?.Model == null)
            return;

        var wagonName = SelectedGoodsWagon.Name;
        SelectedProject.GoodsWagons.Remove(SelectedGoodsWagon);
        SelectedProject.Model.GoodsWagons.Remove((GoodsWagon)SelectedGoodsWagon.Model);
        SelectedGoodsWagon = null;

        _logger.LogInformation("Deleted goods wagon: {Name}", wagonName);
    }

    private bool CanDeleteGoodsWagon() => SelectedGoodsWagon != null;
    #endregion
}
