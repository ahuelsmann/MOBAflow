// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Converter;
using Domain;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Newtonsoft.Json;
using SharedUI.Interface;
using System.Diagnostics;

public class IoService : IIoService
{
    private WindowId? _windowId;
    private XamlRoot? _xamlRoot;
    private readonly ISettingsService _settingsService;
    private readonly IUiDispatcher _uiDispatcher;

    public IoService(ISettingsService settingsService, IUiDispatcher uiDispatcher)
    {
        _settingsService = settingsService;
        _uiDispatcher = uiDispatcher;
    }

    /// <summary>
    /// Sets the WindowId and XamlRoot for the file pickers and dialogs. Must be called before using the service.
    /// </summary>
    public void SetWindowId(WindowId windowId, XamlRoot? xamlRoot = null)
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
        var settings = new JsonSerializerSettings
        {
            Converters = {
                new ActionConverter()
            }
        };

        var sol = JsonConvert.DeserializeObject<Solution>(json, settings) ?? new Solution();

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
            var settings = new JsonSerializerSettings
            {
                Converters = {
                    new ActionConverter()
                }
            };

            var sol = JsonConvert.DeserializeObject<Solution>(json, settings) ?? new Solution();

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

        var settings = new JsonSerializerSettings
        {
            Converters = {
                new ActionConverter()
            },
            Formatting = Formatting.Indented
        };

        var json = JsonConvert.SerializeObject(solution, settings);
        await File.WriteAllTextAsync(path!, json);

        // Save last solution path to settings
        _settingsService.LastSolutionPath = path;

        return (true, path, null);
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
                    Debug.WriteLine(" User cancelled new solution creation");
                    return (false, true, null);
                }

                if (result == ContentDialogResult.Primary)
                {
                    // User wants to save - return and let ViewModel handle save
                    Debug.WriteLine(" User wants to save before creating new solution");
                    return (false, false, "SAVE_REQUESTED");
                }

                // result == Secondary: Don't Save - continue with new solution
                Debug.WriteLine(" User chose not to save - creating new solution");
            }

            Debug.WriteLine(" Creating new empty solution");

            return (true, false, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Failed to create new solution: {ex.Message}");
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

    /// <summary>
    /// Opens a file picker to browse for an XML file (e.g., AnyRail layout).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    public async Task<string?> BrowseForXmlFileAsync()
    {
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        var picker = new FileOpenPicker(_windowId.Value)
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
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        var picker = new FileSavePicker(_windowId.Value)
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
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        var picker = new FileOpenPicker(_windowId.Value)
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
        if (_windowId == null)
            throw new InvalidOperationException("WindowId must be set before using IoService");

        var picker = new FileOpenPicker(_windowId.Value)
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
            // ✅ Use .NET standard APIs for unpackaged WinUI 3 apps
            // ApplicationData.Current only works in packaged apps (MSIX)
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "MOBAflow");
            var photosFolder = Path.Combine(appFolder, "photos");
            var categoryFolder = Path.Combine(photosFolder, category);

            // Create directory structure if it doesn't exist
            Directory.CreateDirectory(categoryFolder);

            // Get file extension from source
            var fileExtension = Path.GetExtension(sourceFilePath);
            var fileName = $"{entityId}{fileExtension}";
            var destinationPath = Path.Combine(categoryFolder, fileName);

            // Copy file asynchronously
            await Task.Run(() => File.Copy(sourceFilePath, destinationPath, overwrite: true));

            // ✅ Return absolute path so Image control can load it
            // Photos are stored in %LOCALAPPDATA%\MOBAflow\photos\{category}\
            return destinationPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving photo: {ex.Message}");
            return null;
        }
    }
}

