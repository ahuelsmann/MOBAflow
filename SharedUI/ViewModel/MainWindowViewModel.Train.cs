// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Microsoft.Extensions.Logging;
using System;

/// <summary>
/// MainWindowViewModel - Train/Locomotive/Wagon Management Partial.
/// </summary>
public partial class MainWindowViewModel
{
    #region Train Selection Properties
    [ObservableProperty]
    private LocomotiveViewModel? selectedLocomotive;

    [ObservableProperty]
    private PassengerWagonViewModel? selectedPassengerWagon;

    [ObservableProperty]
    private GoodsWagonViewModel? selectedGoodsWagon;

    /// <summary>
    /// The currently selected object for TrainsPage properties panel.
    /// Displays: SelectedLocomotive, SelectedPassengerWagon, SelectedGoodsWagon, or SelectedTrain
    /// </summary>
    [ObservableProperty]
    private object? trainsPageSelectedObject;

    /// <summary>
    /// Search text for filtering locomotives.
    /// </summary>
    [ObservableProperty]
    private string locomotiveSearchText = string.Empty;

    /// <summary>
    /// Search text for filtering passenger wagons.
    /// </summary>
    [ObservableProperty]
    private string passengerWagonSearchText = string.Empty;

    /// <summary>
    /// Search text for filtering goods wagons.
    /// </summary>
    [ObservableProperty]
    private string goodsWagonSearchText = string.Empty;

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? value)
    {
        TrainsPageSelectedObject = value;
        AttachLocomotivePhotoCommand(value);
        DeleteLocomotiveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPassengerWagonChanged(PassengerWagonViewModel? value)
    {
        TrainsPageSelectedObject = value;
        AttachWagonPhotoCommand(value, "passenger-wagons");
        DeletePassengerWagonCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedGoodsWagonChanged(GoodsWagonViewModel? value)
    {
        TrainsPageSelectedObject = value;
        AttachWagonPhotoCommand(value, "goods-wagons");
        DeleteGoodsWagonCommand.NotifyCanExecuteChanged();
    }

    private void AttachWagonPhotoCommand(WagonViewModel? wagonVm, string category)
    {
        if (wagonVm == null || _ioService == null) return;
        if (wagonVm.BrowsePhotoCommand != null) return;

        wagonVm.BrowsePhotoCommand = new AsyncRelayCommand(async () =>
        {
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;

            var saved = await _ioService.SavePhotoAsync(photoPath, category, wagonVm.Model.Id);
            if (saved != null)
            {
                wagonVm.PhotoPath = saved;
                HasUnsavedChanges = true;
                _logger.LogInformation("Photo saved for wagon: {Name}", wagonVm.Name);
            }
        });
    }

    private void AttachLocomotivePhotoCommand(LocomotiveViewModel? locoVm)
    {
        if (locoVm == null || _ioService == null) return;
        if (locoVm.BrowsePhotoCommand != null) return;

        locoVm.BrowsePhotoCommand = new AsyncRelayCommand(async () =>
        {
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;

            var saved = await _ioService.SavePhotoAsync(photoPath, "locomotives", locoVm.Model.Id);
            if (saved != null)
            {
                locoVm.PhotoPath = saved;
                HasUnsavedChanges = true;
                _logger.LogInformation("Photo saved for locomotive: {Name}", locoVm.Name);
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
        SelectedLocomotive = new LocomotiveViewModel(locomotive);
        AttachLocomotivePhotoCommand(SelectedLocomotive);
        HasUnsavedChanges = true;

        _logger.LogInformation("Added new locomotive: {Name}", locomotive.Name);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLocomotive))]
    private void DeleteLocomotive()
    {
        if (SelectedLocomotive?.Model == null || SelectedProject?.Model == null)
            return;

        var locomotiveName = SelectedLocomotive.Name;
        SelectedProject.Model.Locomotives.Remove(SelectedLocomotive.Model);
        SelectedLocomotive = null;
        HasUnsavedChanges = true;

        _logger.LogInformation("Deleted locomotive: {Name}", locomotiveName);
    }

    private bool CanDeleteLocomotive() => SelectedLocomotive != null;
    #endregion

    #region PassengerWagon Commands
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
        SelectedPassengerWagon = new PassengerWagonViewModel(wagon);
        AttachWagonPhotoCommand(SelectedPassengerWagon, "passenger-wagons");
        HasUnsavedChanges = true;

        _logger.LogInformation("Added new passenger wagon: {Name}", wagon.Name);
    }

    [RelayCommand(CanExecute = nameof(CanDeletePassengerWagon))]
    private void DeletePassengerWagon()
    {
        if (SelectedPassengerWagon?.Model == null || SelectedProject?.Model == null)
            return;

        var wagonName = SelectedPassengerWagon.Name;
        SelectedProject.Model.PassengerWagons.Remove((PassengerWagon)SelectedPassengerWagon.Model);
        SelectedPassengerWagon = null;
        HasUnsavedChanges = true;

        _logger.LogInformation("Deleted passenger wagon: {Name}", wagonName);
    }

    private bool CanDeletePassengerWagon() => SelectedPassengerWagon != null;
    #endregion

    #region GoodsWagon Commands
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
        SelectedGoodsWagon = new GoodsWagonViewModel(wagon);
        AttachWagonPhotoCommand(SelectedGoodsWagon, "goods-wagons");
        HasUnsavedChanges = true;

        _logger.LogInformation("Added new goods wagon: {Name}", wagon.Name);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteGoodsWagon))]
    private void DeleteGoodsWagon()
    {
        if (SelectedGoodsWagon?.Model == null || SelectedProject?.Model == null)
            return;

        var wagonName = SelectedGoodsWagon.Name;
        SelectedProject.Model.GoodsWagons.Remove((GoodsWagon)SelectedGoodsWagon.Model);
        SelectedGoodsWagon = null;
        HasUnsavedChanges = true;

        _logger.LogInformation("Deleted goods wagon: {Name}", wagonName);
    }

    private bool CanDeleteGoodsWagon() => SelectedGoodsWagon != null;
    #endregion

    #region Photo Commands
    [RelayCommand(CanExecute = nameof(CanBrowseLocomotivePhoto))]
    private async Task BrowseLocomotivePhotoAsync()
    {
        if (SelectedLocomotive?.Model == null) return;

        var photoPath = await _ioService.BrowseForPhotoAsync();
        if (string.IsNullOrEmpty(photoPath)) return;

        // Save photo to local storage
        var savedPath = await _ioService.SavePhotoAsync(photoPath, "locomotives", SelectedLocomotive.Model.Id);
        if (savedPath != null)
        {
            SelectedLocomotive.PhotoPath = savedPath;
            HasUnsavedChanges = true;
            _logger.LogInformation("Photo saved for locomotive: {Name}", SelectedLocomotive.Name);
        }
    }

    private bool CanBrowseLocomotivePhoto() => SelectedLocomotive != null;

    [RelayCommand(CanExecute = nameof(CanBrowsePassengerWagonPhoto))]
    private async Task BrowsePassengerWagonPhotoAsync()
    {
        if (SelectedPassengerWagon?.Model == null) return;

        var photoPath = await _ioService.BrowseForPhotoAsync();
        if (string.IsNullOrEmpty(photoPath)) return;

        // Save photo to local storage
        var savedPath = await _ioService.SavePhotoAsync(photoPath, "passenger-wagons", SelectedPassengerWagon.Model.Id);
        if (savedPath != null)
        {
            SelectedPassengerWagon.PhotoPath = savedPath;
            HasUnsavedChanges = true;
            _logger.LogInformation("Photo saved for passenger wagon: {Name}", SelectedPassengerWagon.Name);
        }
    }

    private bool CanBrowsePassengerWagonPhoto() => SelectedPassengerWagon != null;

    [RelayCommand(CanExecute = nameof(CanBrowseGoodsWagonPhoto))]
    private async Task BrowseGoodsWagonPhotoAsync()
    {
        if (SelectedGoodsWagon?.Model == null) return;

        var photoPath = await _ioService.BrowseForPhotoAsync();
        if (string.IsNullOrEmpty(photoPath)) return;

        // Save photo to local storage
        var savedPath = await _ioService.SavePhotoAsync(photoPath, "goods-wagons", SelectedGoodsWagon.Model.Id);
        if (savedPath != null)
        {
            SelectedGoodsWagon.PhotoPath = savedPath;
            HasUnsavedChanges = true;
            _logger.LogInformation("Photo saved for goods wagon: {Name}", SelectedGoodsWagon.Name);
        }
    }

    private bool CanBrowseGoodsWagonPhoto() => SelectedGoodsWagon != null;

    [RelayCommand]
    private async Task BrowseCurrentWagonPhotoAsync()
    {
        if (_ioService == null) return;

        if (SelectedPassengerWagon?.Model != null)
        {
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;
            var saved = await _ioService.SavePhotoAsync(photoPath, "passenger-wagons", SelectedPassengerWagon.Model.Id);
            if (saved != null)
            {
                SelectedPassengerWagon.PhotoPath = saved;
                HasUnsavedChanges = true;
                _logger.LogInformation("Photo saved for passenger wagon: {Name}", SelectedPassengerWagon.Name);
            }
            return;
        }

        if (SelectedGoodsWagon?.Model != null)
        {
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;
            var saved = await _ioService.SavePhotoAsync(photoPath, "goods-wagons", SelectedGoodsWagon.Model.Id);
            if (saved != null)
            {
                SelectedGoodsWagon.PhotoPath = saved;
                HasUnsavedChanges = true;
                _logger.LogInformation("Photo saved for goods wagon: {Name}", SelectedGoodsWagon.Name);
            }
        }
    }

    [RelayCommand]
    private async Task BrowseSelectedPhotoAsync()
    {
        if (_ioService == null) return;

        if (SelectedLocomotive != null)
        {
            await BrowseLocomotivePhotoAsync();
            return;
        }
        if (SelectedPassengerWagon != null)
        {
            await BrowsePassengerWagonPhotoAsync();
            return;
        }
        if (SelectedGoodsWagon != null)
        {
            await BrowseGoodsWagonPhotoAsync();
        }
    }
    #endregion
}
