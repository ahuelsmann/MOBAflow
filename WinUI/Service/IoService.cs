// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;
using Backend.Model;

using Microsoft.Windows.Storage.Pickers;

using Moba.SharedUI.Service;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class IoService : IIoService
{
    private Microsoft.UI.WindowId? _windowId;
    private readonly IPreferencesService _preferencesService;

    public IoService(IPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    /// <summary>
    /// Sets the WindowId for the file pickers. Must be called before using the service.
    /// </summary>
    public void SetWindowId(Microsoft.UI.WindowId windowId)
    {
        _windowId = windowId;
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

        var sol = new Solution();
        sol = await sol.LoadAsync(result.Path);
        
        // Save last solution path to preferences
        _preferencesService.LastSolutionPath = result.Path;
        
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

            var sol = new Solution();
            sol = await sol.LoadAsync(filePath);
            
            // Save last solution path to preferences
            _preferencesService.LastSolutionPath = filePath;
            
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
            if (!_preferencesService.AutoLoadLastSolution)
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ Auto-load is disabled in preferences");
                return (null, null, null);
            }

            // Check if there's a last solution path
            var lastPath = _preferencesService.LastSolutionPath;
            if (string.IsNullOrEmpty(lastPath))
            {
                System.Diagnostics.Debug.WriteLine("ℹ️ No previous solution path found");
                return (null, null, null);
            }

            // Check if the file still exists
            if (!File.Exists(lastPath))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Last solution file not found: {lastPath}");
                return (null, null, $"Last solution file not found: {lastPath}");
            }

            System.Diagnostics.Debug.WriteLine($"🔄 Auto-loading last solution: {lastPath}");
            
            // Load the solution
            var sol = new Solution();
            sol = await sol.LoadAsync(lastPath);
            
            System.Diagnostics.Debug.WriteLine($"✅ Auto-loaded solution with {sol.Projects.Count} projects");
            return (sol, lastPath, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to auto-load last solution: {ex.Message}");
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

        await Solution.SaveAsync(path!, solution);
        
        // Save last solution path to preferences
        _preferencesService.LastSolutionPath = path;
        
        return (true, path, null);
    }

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
}