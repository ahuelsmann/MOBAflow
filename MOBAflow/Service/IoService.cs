// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Service;

using Common.Configuration;
using Common.Path;
using Common.Validation;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

using SharedUI.Interface;

using System.Text.Json;

internal class IoService : IIoService
{
    private WindowId? _windowId;
    private XamlRoot? _xamlRoot;
    private readonly AppSettings _appSettings;
    private readonly ISettingsService _settingsService;
    private readonly IProjectValidator _projectValidator;
    private readonly ILogger<IoService> _logger;

    public IoService(AppSettings appSettings, ISettingsService settingsService, IProjectValidator projectValidator, ILogger<IoService> logger)
    {
        _appSettings = appSettings;
        _settingsService = settingsService;
        _projectValidator = projectValidator;
        _logger = logger;
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
            SettingsIdentifier = "MobaSolutionPicker",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            Title = "Open MOBAflow Solution",
            FileTypeChoices = { { "MOBAflow Solution (*.json)", new List<string> { ".json" } } }
        };

        var result = await picker.PickSingleFileAsync();
        if (result == null) return (null, null, null);

        var json = await File.ReadAllTextAsync(result.Path);

        // ✅ Early validation: Detect non-JSON file formats
        var formatError = ValidateFileFormat(json, result.Path);
        if (formatError != null)
        {
            return (null, null, formatError);
        }

        // ✅ Validate JSON before deserialization
        var validationResult = JsonValidationService.Validate(json, Solution.CurrentSchemaVersion);
        if (!validationResult.IsValid)
        {
            return (null, null, $"Invalid solution file: {validationResult.ErrorMessage}");
        }

        try
        {
            var sol = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default) ?? new Solution();
            NormalizeLoadedPhotoPaths(sol);
            PhotoPathHelper.SetSolutionDirectory(result.Path);

            // ✅ Validate project completeness after loading
            var completenessResult = _projectValidator.ValidateCompleteness(sol);
            if (completenessResult.HasWarnings || completenessResult.HasErrors)
            {
                _logger.LogInformation("[Project Validation] {Summary}", completenessResult.GetSummary());
                foreach (var msg in completenessResult.Messages)
                {
                    switch (msg.Level)
                    {
                        case ValidationLevel.Error:
                            _logger.LogError(msg.Text);
                            break;
                        case ValidationLevel.Warning:
                            _logger.LogWarning(msg.Text);
                            break;
                        default:
                            _logger.LogInformation(msg.Text);
                            break;
                    }
                }
            }

            // Save last solution path to settings
            _settingsService.LastSolutionPath = result.Path;

            return (sol, result.Path, null);
        }
        catch (JsonException ex)
        {
            return (null, null, $"Failed to parse JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, null, $"Failed to load solution: {ex.Message}");
        }
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

            // ✅ Validate JSON before deserialization
            var validationResult = JsonValidationService.Validate(json, Solution.CurrentSchemaVersion);
            if (!validationResult.IsValid)
            {
                return (null, null, $"Invalid solution file: {validationResult.ErrorMessage}");
            }

            var sol = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default) ?? new Solution();
            NormalizeLoadedPhotoPaths(sol);
            PhotoPathHelper.SetSolutionDirectory(filePath);

            // Save last solution path to settings
            _settingsService.LastSolutionPath = filePath;

            return (sol, filePath, null);
        }
        catch (JsonException ex)
        {
            return (null, null, $"Failed to parse JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, null, $"Error loading solution: {ex.Message}");
        }
    }

    public Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string currentPath)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPath);
        return SaveToPathAsync(solution, currentPath);
    }

    public async Task<(bool success, string? path, string? error)> SaveAsAsync(Solution solution)
    {
        EnsureInitialized();

        var picker = new FileSavePicker(_windowId.GetValueOrDefault())
        {
            SettingsIdentifier = "MobaSolutionSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "solution",
            DefaultFileExtension = ".json",
            ShowOverwritePrompt = true,
            Title = "Save MOBAflow Solution",
            FileTypeChoices = { { "MOBAflow Solution (*.json)", new List<string> { ".json" } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result == null
            ? (false, null, null)
            : await SaveToPathAsync(solution, result.Path).ConfigureAwait(false);
    }

    private async Task<(bool success, string? path, string? error)> SaveToPathAsync(Solution solution, string path)
    {
        try
        {
            var json = JsonSerializer.Serialize(solution, JsonOptions.Default);

            // ✅ Atomic write: Write to temp file first, then rename to avoid data corruption
            var tempPath = path + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, path, overwrite: true);

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
            SettingsIdentifier = "MobaJsonPicker",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            Title = "Select JSON File",
            FileTypeChoices = { { "JSON Files (*.json)", new List<string> { ".json" } } }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <inheritdoc />
    public async Task<string?> BrowseForRecordingFileAsync()
    {
        EnsureInitialized();

        var picker = new FileOpenPicker(_windowId.GetValueOrDefault())
        {
            SettingsIdentifier = "MobaRecordingPicker",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            Title = "Open MOBAflow Recording",
            FileTypeChoices = { { "MOBAflow Recording (*.mobarecording.json)", new List<string> { ".json" } } }
        };

        var result = await picker.PickSingleFileAsync();
        return result?.Path;
    }

    /// <inheritdoc />
    public async Task<string?> SaveRecordingFileAsync(string suggestedFileName)
    {
        EnsureInitialized();

        var picker = new FileSavePicker(_windowId.GetValueOrDefault())
        {
            SettingsIdentifier = "MobaRecordingSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"{suggestedFileName}.mobarecording",
            DefaultFileExtension = ".json",
            ShowOverwritePrompt = true,
            Title = "Save MOBAflow Recording",
            FileTypeChoices = { { "MOBAflow Recording (*.mobarecording.json)", new List<string> { ".json" } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result?.Path;
    }

    /// <summary>
    /// Opens a file save picker for saving a JSON file.
    /// </summary>
    public async Task<string?> SaveJsonFileAsync(string suggestedFileName)
    {
        EnsureInitialized();

        var picker = new FileSavePicker(_windowId.GetValueOrDefault())
        {
            SettingsIdentifier = "MobaJsonSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = ".json",
            ShowOverwritePrompt = true,
            Title = "Save JSON File",
            FileTypeChoices = { { "JSON Files (*.json)", new List<string> { ".json" } } }
        };

        var result = await picker.PickSaveFileAsync();
        return result?.Path;
    }

    public async Task<string?> SaveHtmlFileAsync(string suggestedFileName)
    {
        EnsureInitialized();

        var picker = new FileSavePicker(_windowId.GetValueOrDefault())
        {
            SettingsIdentifier = "MobaHtmlSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = ".html",
            ShowOverwritePrompt = true,
            Title = "Save Locomotive Passport",
            FileTypeChoices = { { "HTML Documents (*.html)", new List<string> { ".html" } } }
        };

        var result = await picker.PickSaveFileAsync();
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
            SettingsIdentifier = "MobaXmlPicker",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            Title = "Import AnyRail Layout",
            FileTypeChoices = { { "AnyRail Layout (*.xml)", new List<string> { ".xml" } } }
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
            SettingsIdentifier = "MobaXmlSaver",
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
            DefaultFileExtension = ".xml",
            ShowOverwritePrompt = true,
            Title = "Save XML File",
            FileTypeChoices = { { "XML Files (*.xml)", new List<string> { ".xml" } } }
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
            SettingsIdentifier = "MobaAudioPicker",
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            Title = "Select Audio File",
            FileTypeChoices = { { "Audio Files", new List<string> { ".wav", ".mp3", ".ogg", ".flac", ".m4a" } } }
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
            SettingsIdentifier = "MobaPhotoPicker",
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            Title = "Select Photo",
            FileTypeChoices = { { "Image Files", new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".gif" } } }
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
            var targetCategory = NormalizePhotoCategory(category);
            var baseDir = GetPhotoBaseDir();
            var fileExtension = Path.GetExtension(sourceFilePath);
            var relativePath = PhotoPathHelper.ToStorageRelativePath(targetCategory, entityId, fileExtension);
            var destinationPath = PhotoPathHelper.ToFullPath(baseDir, relativePath);
            var categoryFolder = Path.GetDirectoryName(destinationPath);

            if (string.IsNullOrWhiteSpace(categoryFolder))
                return null;

            Directory.CreateDirectory(categoryFolder);

            await FileCopyAsync(sourceFilePath, destinationPath);

            return relativePath;
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

    private static string NormalizePhotoCategory(string category)
    {
        return PhotoPathHelper.NormalizeCategory(category);
    }

    /// <summary>
    /// Converts a relative photo path to an absolute file system path.
    /// Uses Application.PhotoStoragePath when set, otherwise My Documents\MOBAflow\Photos.
    /// </summary>
    public string? GetPhotoFullPath(string? relativePath)
    {
        return PhotoPathHelper.TryResolveExistingPhotoFullPath(
            _appSettings.Application.PhotoStoragePath,
            relativePath);
    }

    private string GetPhotoBaseDir()
    {
        return PhotoPathHelper.ResolvePhotoBaseDirectory(_appSettings.Application.PhotoStoragePath);
    }

    private static string GetLegacyPhotoBaseDir()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "MOBAflow", "photos");
    }

    private void NormalizeLoadedPhotoPaths(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var locomotive in project.Locomotives)
                locomotive.PhotoPath = NormalizeLoadedPhotoPath(locomotive.PhotoPath);

            foreach (var passengerWagon in project.PassengerWagons)
                passengerWagon.PhotoPath = NormalizeLoadedPhotoPath(passengerWagon.PhotoPath);

            foreach (var goodsWagon in project.GoodsWagons)
                goodsWagon.PhotoPath = NormalizeLoadedPhotoPath(goodsWagon.PhotoPath);
        }
    }

    private string? NormalizeLoadedPhotoPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var normalizedPath = PhotoPathHelper.NormalizeStoredRelativePath(path);
        if (!Path.IsPathRooted(normalizedPath))
            return normalizedPath;

        if (PhotoPathHelper.TryGetStorageRelativePath(GetPhotoBaseDir(), normalizedPath, out var currentRelativePath))
            return currentRelativePath;

        if (PhotoPathHelper.TryGetStorageRelativePath(GetLegacyPhotoBaseDir(), normalizedPath, out var legacyRelativePath))
            return legacyRelativePath;

        return normalizedPath;
    }

    /// <summary>
    /// Validates that the file content is valid JSON format.
    /// </summary>
    /// <returns>Error message if invalid, null if valid.</returns>
    private static string? ValidateFileFormat(string content, string filePath)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"File is empty: {filePath}";
        }

        // Basic check: JSON files should start with '{' or '['
        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
        {
            return $"File does not appear to be valid JSON: {filePath}";
        }

        return null;
    }
}
