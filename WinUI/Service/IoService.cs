// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Domain;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

using SharedUI.Interface;

using System.Text.Json;

public class IoService : IIoService
{
    private WindowId? _windowId;
    private XamlRoot? _xamlRoot;
    private readonly ISettingsService _settingsService;

    public IoService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Sets the WindowId and XamlRoot for the file pickers and dialogs. Must be called before using the service.
    /// </summary>
    public void SetWindowId(WindowId windowId, XamlRoot? xamlRoot = null)
    {
        _windowId = windowId;
        _xamlRoot = xamlRoot;
    }

    /// <summary>
    /// Ensures the service is initialized with a WindowId before file operations.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_windowId.HasValue)
        {
            throw new InvalidOperationException("WindowId must be set before using IoService. Call SetWindowId() first.");
        }
    }

    public async Task<(Solution? solution, string? path, string? error)> LoadAsync()
    {
        EnsureInitialized();

        var picker = new FileOpenPicker(_windowId.GetValueOrDefault())
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ".json" }
        };

        var result = await picker.PickSingleFileAsync();
        if (result == null) return (null, null, null);

        var json = await File.ReadAllTextAsync(result.Path);
        var sol = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default) ?? new Solution();

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
            var sol = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default) ?? new Solution();

            // Save last solution path to settings
            _settingsService.LastSolutionPath = filePath;

            return (sol, filePath, null);
        }
        catch (Exception ex)
        {
            return (null, null, $"Error loading solution: {ex.Message}");
        }
    }

    public async Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath)
    {
        EnsureInitialized();

        string? path = currentPath;
        if (string.IsNullOrEmpty(path))
        {
            var picker = new FileSavePicker(_windowId.GetValueOrDefault())
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

        try
        {
            var json = JsonSerializer.Serialize(solution, JsonOptions.Default);
            
            // ✅ Atomic write: Write to temp file first, then rename to avoid data corruption
            var tempPath = path! + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, path!, overwrite: true);

            // Save last solution path to settings
            _settingsService.LastSolutionPath = path;

            return (true, path, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Save failed: {ex.Message}");
        }
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
                EnsureInitialized();

                var dialog = new ContentDialog
                {
                    Title = "Unsaved Changes",
                    Content = "You have unsaved changes in the current solution. Do you want to save before creating a new solution?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = _xamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.None)
                {
                    // User cancelled
                    return (false, true, null);
                }

                if (result == ContentDialogResult.Primary)
                {
                    // User wants to save - return and let ViewModel handle save
                    return (false, false, "SAVE_REQUESTED");
                }

                // result == Secondary: Don't Save - continue with new solution
            }

            return (true, false, null);
        }
        catch (Exception ex)
        {
            return (false, false, $"Failed to create new solution: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens a file picker to browse for a JSON file.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForJsonFileAsync()
    {
        EnsureInitialized();

        var picker = new FileOpenPicker(_windowId.GetValueOrDefault())
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ".json" }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file picker to browse for an XML file (e.g., AnyRail layout).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForXmlFileAsync()
    {
        EnsureInitialized();

        var picker = new FileOpenPicker(_windowId.GetValueOrDefault())
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ".xml" }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file save picker for saving an XML file.
    /// </summary>
    public async Task<string?> SaveXmlFileAsync(string suggestedFileName)
    {
        EnsureInitialized();

        var picker = new FileSavePicker(_windowId.GetValueOrDefault())
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = ".xml",
            FileTypeChoices = { { "XML Files", new List<string> { ".xml" } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file picker to browse for an audio file (WAV, MP3, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForAudioFileAsync()
    {
        EnsureInitialized();

        var picker = new FileOpenPicker(_windowId.GetValueOrDefault())
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            FileTypeFilter = { ".wav", ".mp3", ".ogg", ".flac", ".m4a" }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file picker to browse for a photo/image file (JPG, PNG, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForPhotoAsync()
    {
        EnsureInitialized();

        var picker = new FileOpenPicker(_windowId.GetValueOrDefault())
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            FileTypeFilter = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Saves a photo file to the application's local photos storage.
    /// Creates folder structure: %LOCALAPPDATA%\MOBAflow\photos\{category}\{entityId}.ext
    /// </summary>
    /// <param name="sourceFilePath">Source photo file path</param>
    /// <param name="category">Photo category (e.g., "locomotives", "passenger-wagons", "goods-wagons")</param>
    /// <param name="entityId">Entity ID for filename</param>
    /// <returns>Absolute path to saved photo (e.g., "C:\Users\...\AppData\Local\MOBAflow\photos\locomotives\{id}.jpg")</returns>
    public async Task<string?> SavePhotoAsync(string sourceFilePath, string category, Guid entityId)
    {
        try
        {
            // ✅ Normalize category to standard folder names
            var targetCategory = NormalizePhotoCategory(category);

            // ✅ Use .NET standard APIs for unpackaged WinUI 3 apps
            // ApplicationData.Current only works in packaged apps (MSIX)
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "MOBAflow");
            var photosFolder = Path.Combine(appFolder, "photos");
            var categoryFolder = Path.Combine(photosFolder, targetCategory);

            // Create directory structure if it doesn't exist
            Directory.CreateDirectory(categoryFolder);

            // Get file extension from source
            var fileExtension = Path.GetExtension(sourceFilePath);
            var fileName = $"{entityId}{fileExtension}";
            var destinationPath = Path.Combine(categoryFolder, fileName);

            // Copy file asynchronously
            await FileCopyAsync(sourceFilePath, destinationPath);

            // ✅ Return absolute path so Image control can load it
            return destinationPath;
        }
        catch
        {
            return null;
        }
    }

    private static async Task FileCopyAsync(string sourceFilePath, string destinationPath)
    {
        const int bufferSize = 81920;
        await using var source = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        await using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
        await source.CopyToAsync(destination);
    }

    /// <summary>
    /// Normalizes photo category to standard folder names.
    /// </summary>
    private static string NormalizePhotoCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "locomotives" => "locomotives",
            "passenger-wagons" or "goods-wagons" => "wagons",
            _ => throw new ArgumentException($"Unknown photo category: '{category}'. Valid values: 'locomotives', 'passenger-wagons', 'goods-wagons'", nameof(category))
        };
    }

    /// <summary>
    /// Converts a relative photo path to an absolute file system path.
    /// </summary>
    public string? GetPhotoFullPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        // If already absolute, return as-is
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        // Convert relative path to absolute
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localAppData, "MOBAflow");

        // ✅ Platform-agnostic: Normalize path separators
        // relativePath format: "photos/locomotives/{guid}.jpg" or "photos\locomotives\{guid}.jpg"
        var normalizedPath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString());
        var absolutePath = Path.Combine(appFolder, normalizedPath);

        return absolutePath;
    }
}

