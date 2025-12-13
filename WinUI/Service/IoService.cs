// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Windows.Storage.Pickers;

using Backend.Data;
using Domain;
using SharedUI.Interface;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class IoService : IIoService
{
    private Microsoft.UI.WindowId? _windowId;
    private Microsoft.UI.Xaml.XamlRoot? _xamlRoot;
    private readonly ISettingsService _settingsService;

    public IoService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Sets the WindowId and XamlRoot for the file pickers and dialogs. Must be called before using the service.
    /// </summary>
    public void SetWindowId(Microsoft.UI.WindowId windowId, Microsoft.UI.Xaml.XamlRoot? xamlRoot = null)
    {
        _windowId = windowId;
        _xamlRoot = xamlRoot;
    }

    public async Task<(Solution? solution, string? path, string? error)> LoadAsync()
    {
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        var picker = new FileOpenPicker(_windowId.Value)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ".json" }
        };

        var result = await picker.PickSingleFileAsync();
        if (result == null) return (null, null, null);

        var json = await File.ReadAllTextAsync(result.Path);
        
        // Configure serialization with ActionConverter
        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            Converters = {
                new Backend.Converter.ActionConverter()
            }
        };
        
        var sol = Newtonsoft.Json.JsonConvert.DeserializeObject<Solution>(json, settings) ?? new Solution();
        
        // Save last solution path to settings
        _settingsService.LastSolutionPath = result.Path;
        
        return (sol, result.Path, null);
    }

    /// <summary>
    /// Loads a solution from a specific path without showing a file picker.
    /// Used for auto-loading or programmatic loading.
    /// </summary>
    public async Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return (null, null, $"File not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            
            // Configure serialization with ActionConverter
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Converters = {
                    new Backend.Converter.ActionConverter()
                }
            };
            
            var sol = Newtonsoft.Json.JsonConvert.DeserializeObject<Solution>(json, settings) ?? new Solution();
            
            // Save last solution path to settings
            _settingsService.LastSolutionPath = filePath;
            
            return (sol, filePath, null);
        }
        catch (Exception ex)
        {
            return (null, null, $"Error loading solution: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to auto-load the last opened solution if auto-load is enabled.
    /// Returns null if auto-load is disabled, no previous solution exists, or the file is no longer available.
    /// </summary>
    public async Task<(Solution? solution, string? path, string? error)> TryAutoLoadLastSolutionAsync()
    {
        try
        {
            // Check if auto-load is enabled
            if (!_settingsService.AutoLoadLastSolution)
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ Auto-load is disabled in settings");
                return (null, null, null);
            }

            // Check if there's a last solution path
            var lastPath = _settingsService.LastSolutionPath;
            if (string.IsNullOrEmpty(lastPath))
            {
                System.Diagnostics.Debug.WriteLine(" No previous solution path found");
                return (null, null, null);
            }

            // Check if the file still exists
            if (!File.Exists(lastPath))
            {
                System.Diagnostics.Debug.WriteLine($" Last solution file not found: {lastPath}");
                return (null, null, $"Last solution file not found: {lastPath}");
            }

            System.Diagnostics.Debug.WriteLine($" Auto-loading last solution: {lastPath}");
            
            var json = await File.ReadAllTextAsync(lastPath!);
            
            // Configure serialization with ActionConverter
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Converters = {
                    new Backend.Converter.ActionConverter()
                }
            };
            
            var loadedSolution = Newtonsoft.Json.JsonConvert.DeserializeObject<Solution>(json, settings) ?? new Solution();
            
            if (loadedSolution == null)
            {
                System.Diagnostics.Debug.WriteLine($" Failed to load solution from {lastPath}");
                return (null, null, $"Failed to load solution from {lastPath}");
            }
            
            System.Diagnostics.Debug.WriteLine($" Auto-loaded solution with {loadedSolution.Projects.Count} projects");
            return (loadedSolution, lastPath, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Failed to auto-load last solution: {ex.Message}");
            return (null, null, $"Failed to auto-load: {ex.Message}");
        }
    }

    public async Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath)
    {
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        string? path = currentPath;
        if (string.IsNullOrEmpty(path))
        {
            var picker = new FileSavePicker(_windowId.Value)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "solution",
                DefaultFileExtension = ".json",
                FileTypeChoices = { { "JSON", new List<string> { ".json" } } }
            };

            var result = await picker.PickSaveFileAsync();
            if (result == null) return (false, null, null);
            path = result.Path;
        }

        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            Converters = {
                new Backend.Converter.ActionConverter()
            },
            Formatting = Newtonsoft.Json.Formatting.Indented
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(solution, settings);
        await File.WriteAllTextAsync(path!, json);
        
        // Save last solution path to settings
        _settingsService.LastSolutionPath = path;
        
        return (true, path, null);
    }

    /// <summary>
    /// Loads city master data using legacy Backend.Data.DataManager format.
    /// This method is obsolete - use ICityService.LoadCitiesAsync() instead.
    /// </summary>
    [Obsolete("Use ICityService.LoadCitiesAsync() instead. This method loads deprecated Backend.Data.City format.")]
    public async Task<(DataManager? dataManager, string? path, string? error)> LoadDataManagerAsync()
    {
        // Try to load default germany-stations.json from application directory
        var appDirectory = AppContext.BaseDirectory;
        var defaultPath = Path.Combine(appDirectory, "germany-stations.json");

        if (File.Exists(defaultPath))
        {
            var dataManager = await DataManager.LoadAsync(defaultPath);
            return (dataManager, defaultPath, null);
        }

        // If default file not found, open file picker
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        var picker = new FileOpenPicker(_windowId.Value)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ".json" }
        };

        var result = await picker.PickSingleFileAsync();
        if (result == null) return (null, null, "No file selected");

        var dm = await DataManager.LoadAsync(result.Path);
        return (dm, result.Path, null);
    }

    /// <summary>
    /// Creates a new empty solution.
    /// Prompts user for confirmation if unsaved changes exist.
    /// </summary>
    public async Task<(bool success, bool userCancelled, string? error)> NewSolutionAsync(bool hasUnsavedChanges)
    {
        try
        {
            // Check if there are unsaved changes
            if (hasUnsavedChanges)
            {
                if (_windowId == null)
                    throw new InvalidOperationException("WindowId must be set before using IoService");

                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Unsaved Changes",
                    Content = "You have unsaved changes in the current solution. Do you want to save before creating a new solution?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                    XamlRoot = _xamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.None)
                {
                    // User cancelled
                    System.Diagnostics.Debug.WriteLine(" User cancelled new solution creation");
                    return (false, true, null);
                }

                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    // User wants to save - return and let ViewModel handle save
                    System.Diagnostics.Debug.WriteLine(" User wants to save before creating new solution");
                    return (false, false, "SAVE_REQUESTED");
                }

                // result == Secondary: Don't Save - continue with new solution
                System.Diagnostics.Debug.WriteLine(" User chose not to save - creating new solution");
            }
            
            System.Diagnostics.Debug.WriteLine(" Creating new empty solution");
            
            return (true, false, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Failed to create new solution: {ex.Message}");
            return (false, false, $"Failed to create new solution: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens a file picker to browse for a JSON file.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForJsonFileAsync()
    {
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        var picker = new FileOpenPicker(_windowId.Value)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ".json" }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }
}
