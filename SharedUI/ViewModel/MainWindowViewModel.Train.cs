// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

/// <summary>
/// MainWindowViewModel - Train/Locomotive/Wagon Management Partial.
/// </summary>
public partial class MainWindowViewModel
{
    #region Train Selection Properties
    [ObservableProperty]
    private LocomotiveViewModel? _selectedLocomotive;

    [ObservableProperty]
    private PassengerWagonViewModel? _selectedPassengerWagon;

    [ObservableProperty]
    private GoodsWagonViewModel? _selectedGoodsWagon;

    /// <summary>
    /// The currently selected object for TrainsPage properties panel.
    /// Displays: SelectedLocomotive, SelectedPassengerWagon, SelectedGoodsWagon, or SelectedTrain
    /// </summary>
    [ObservableProperty]
    private object? _trainsPageSelectedObject;

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
        TrainsPageSelectedObject = value;
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
        TrainsPageSelectedObject = value;
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
        TrainsPageSelectedObject = value;
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
        SelectedLocomotive = new LocomotiveViewModel(locomotive);
        AttachLocomotivePhotoCommand(SelectedLocomotive);

        _logger.LogInformation("Added new locomotive: {Name}", locomotive.Name);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLocomotive))]
    private void DeleteLocomotive()
    {
        if (SelectedLocomotive?.Model == null || SelectedProject?.Model == null)
        {
            return;
        }

        var locomotiveName = SelectedLocomotive.Name;
        SelectedProject.Model.Locomotives.Remove(SelectedLocomotive.Model);
        SelectedLocomotive = null;

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
        AttachWagonPhotoCommand(SelectedPassengerWagon);

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
        AttachWagonPhotoCommand(SelectedGoodsWagon);

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
        var savedPath = await _ioService.SavePhotoAsync(photoPath, "wagons", SelectedPassengerWagon.Model.Id);
        if (savedPath != null)
        {
            SelectedPassengerWagon.PhotoPath = savedPath;
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
        var savedPath = await _ioService.SavePhotoAsync(photoPath, "wagons", SelectedGoodsWagon.Model.Id);
        if (savedPath != null)
        {
            SelectedGoodsWagon.PhotoPath = savedPath;
            _logger.LogInformation("Photo saved for goods wagon: {Name}", SelectedGoodsWagon.Name);
        }
    }

    private bool CanBrowseGoodsWagonPhoto() => SelectedGoodsWagon != null;

    [RelayCommand]
    private async Task BrowseCurrentWagonPhotoAsync()
    {
        if (SelectedPassengerWagon?.Model != null)
        {
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;
            var saved = await _ioService.SavePhotoAsync(photoPath, "wagons", SelectedPassengerWagon.Model.Id);
            if (saved != null)
            {
                SelectedPassengerWagon.PhotoPath = saved;
                _logger.LogInformation("Photo saved for passenger wagon: {Name}", SelectedPassengerWagon.Name);
            }
            return;
        }

        if (SelectedGoodsWagon?.Model != null)
        {
            var photoPath = await _ioService.BrowseForPhotoAsync();
            if (string.IsNullOrEmpty(photoPath)) return;
            var saved = await _ioService.SavePhotoAsync(photoPath, "wagons", SelectedGoodsWagon.Model.Id);
            if (saved != null)
            {
                SelectedGoodsWagon.PhotoPath = saved;
                _logger.LogInformation("Photo saved for goods wagon: {Name}", SelectedGoodsWagon.Name);
            }
        }
    }

    [RelayCommand]
    private async Task BrowseSelectedPhotoAsync()
    {
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

    #region Photo Upload Notification (SignalR)
    /// <summary>
    /// Assigns the latest uploaded photo to the currently selected locomotive or wagon.
    /// Called by SignalR PhotoHubClient when PhotoUploaded event is received.
    /// </summary>
    public void AssignLatestPhoto(string photoPath)
    {
        try
        {
            Debug.WriteLine($"[PHOTO] AssignLatestPhoto start: {photoPath}");
            Debug.WriteLine($"   SelectedProject: {(SelectedProject != null ? "YES" : "NO")}");
            Debug.WriteLine($"   SelectedLocomotive: {(SelectedLocomotive != null ? "YES" : "NO")}");
            Debug.WriteLine($"   SelectedPassengerWagon: {(SelectedPassengerWagon != null ? "YES" : "NO")}");
            Debug.WriteLine($"   SelectedGoodsWagon: {(SelectedGoodsWagon != null ? "YES" : "NO")}");

            _logger.LogInformation("[PHOTO] AssignLatestPhoto start: {PhotoPath}", photoPath);
            _logger.LogInformation("SelectedProject: {HasProject}", SelectedProject != null ? "YES" : "NO");
            _logger.LogInformation("SelectedLocomotive: {HasLoco}", SelectedLocomotive != null ? "YES" : "NO");
            _logger.LogInformation("SelectedPassengerWagon: {HasPW}", SelectedPassengerWagon != null ? "YES" : "NO");
            _logger.LogInformation("SelectedGoodsWagon: {HasGW}", SelectedGoodsWagon != null ? "YES" : "NO");

            if (SelectedProject?.Model == null)
            {
                Debug.WriteLine("No project selected - cannot assign photo");
                _logger.LogWarning("No project selected - cannot assign photo");
                return;
            }

            if (SelectedLocomotive != null)
            {
                Debug.WriteLine($"Assigning to locomotive: {SelectedLocomotive.Name}");
                _logger.LogInformation("Assigning to locomotive: {Name}", SelectedLocomotive.Name);

                Debug.WriteLine("Calling MovePhotoToCategory...");
                var newPhotoPath = MovePhotoToCategory(photoPath, "locomotives", SelectedLocomotive.Model.Id);
                Debug.WriteLine($"MovePhotoToCategory returned: {newPhotoPath ?? "NULL"}");

                if (newPhotoPath != null)
                {
                    Debug.WriteLine("Photo path valid, setting on ViewModel");
                    SelectedLocomotive.PhotoPath = newPhotoPath;
                    Debug.WriteLine($"Photo assigned to locomotive: {SelectedLocomotive.Name} -> {newPhotoPath}");
                    _logger.LogInformation("Photo assigned to locomotive: {Name} -> {Path}", SelectedLocomotive.Name, newPhotoPath);
                }
                else
                {
                    Debug.WriteLine("Failed to move photo for locomotive (newPhotoPath is NULL)");
                    _logger.LogError("Failed to move photo for locomotive");
                }
            }
            else if (SelectedPassengerWagon != null)
            {
                _logger.LogInformation("Assigning to passenger wagon: {Name}", SelectedPassengerWagon.Name);

                var newPhotoPath = MovePhotoToCategory(photoPath, "wagons", SelectedPassengerWagon.Model.Id);
                if (newPhotoPath != null)
                {
                    SelectedPassengerWagon.PhotoPath = newPhotoPath;
                    _logger.LogInformation("Photo assigned to passenger wagon: {Name} -> {Path}", SelectedPassengerWagon.Name, newPhotoPath);
                }
                else
                {
                    _logger.LogError("Failed to move photo for passenger wagon");
                }
            }
            else if (SelectedGoodsWagon != null)
            {
                _logger.LogInformation("Assigning to goods wagon: {Name}", SelectedGoodsWagon.Name);

                var newPhotoPath = MovePhotoToCategory(photoPath, "wagons", SelectedGoodsWagon.Model.Id);
                if (newPhotoPath != null)
                {
                    SelectedGoodsWagon.PhotoPath = newPhotoPath;
                    _logger.LogInformation("Photo assigned to goods wagon: {Name} -> {Path}", SelectedGoodsWagon.Name, newPhotoPath);
                }
                else
                {
                    _logger.LogError("Failed to move photo for goods wagon");
                }
            }
            else
            {
                _logger.LogWarning("No item selected - cannot assign photo");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign photo: {PhotoPath}", photoPath);
        }
    }

    /// <summary>
    /// Moves photo from temp folder to category folder with entity GUID.
    /// </summary>
    private string? MovePhotoToCategory(string tempPhotoPath, string category, Guid entityId)
    {
        try
        {
            Debug.WriteLine("MovePhotoToCategory start");
            Debug.WriteLine($"   tempPhotoPath: {tempPhotoPath}");
            Debug.WriteLine($"   category: {category}");
            Debug.WriteLine($"   entityId: {entityId}");

            // Get photo storage directory (base directory without "photos" subfolder)
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MOBAflow");
            Debug.WriteLine($"   baseDir: {baseDir}");

            // tempPhotoPath is relative (e.g., "photos/temp/xyz.jpg")
            var tempPath = Path.Combine(baseDir, tempPhotoPath);
            Debug.WriteLine($"   tempPath: {tempPath}");

            if (!File.Exists(tempPath))
            {
                Debug.WriteLine($"Temp photo not found: {tempPath}");
                _logger.LogWarning("Temp photo not found: {Path}", tempPath);
                return null;
            }

            Debug.WriteLine("Temp photo exists");

            // Create category directory (e.g., "C:\\...\\MOBAflow\\photos\\locomotives")
            var categoryDir = Path.Combine(baseDir, "photos", category);
            Debug.WriteLine($"   categoryDir: {categoryDir}");
            Directory.CreateDirectory(categoryDir);
            Debug.WriteLine("Category directory created or verified");

            var extension = Path.GetExtension(tempPath);
            var newFileName = $"{entityId}{extension}";
            var newPath = Path.Combine(categoryDir, newFileName);
            Debug.WriteLine($"   newPath: {newPath}");

            Debug.WriteLine("Moving file");
            File.Move(tempPath, newPath, overwrite: true);
            Debug.WriteLine("File moved successfully");

            var relativePath = Path.Combine(category, newFileName).Replace("\\", "/");
            Debug.WriteLine($"Photo moved: {tempPhotoPath} -> {relativePath}");
            _logger.LogInformation("Photo moved: {OldPath} -> {NewPath}", tempPhotoPath, relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MovePhotoToCategory exception: {ex.Message}");
            Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Failed to move photo: {Path}", tempPhotoPath);
            return null;
        }
    }
    #endregion
}

