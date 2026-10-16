// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Service;

using Common.Configuration;
using Common.Path;
using Common.Validation;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

using SharedUI.Interface;

using System.Text.Json;

/// <summary>
/// Creates, loads and saves MOBAflow solution files on the desktop, including the solution open and save
/// dialogs and the unsaved-changes prompt.
/// </summary>
internal sealed class SolutionFileService : ISolutionIoService
{
    private readonly WinUiWindowContext _window;
    private readonly AppSettings _appSettings;
    private readonly ISettingsService _settingsService;
    private readonly IProjectValidator _projectValidator;
    private readonly ILogger<SolutionFileService> _logger;

    public SolutionFileService(
        WinUiWindowContext window,
        AppSettings appSettings,
        ISettingsService settingsService,
        IProjectValidator projectValidator,
        ILogger<SolutionFileService> logger)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _projectValidator = projectValidator ?? throw new ArgumentNullException(nameof(projectValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool CanPersist => true;

    public async Task<(Solution? solution, string? path, string? error)> LoadAsync()
    {
        var picker = new FileOpenPicker(_window.RequireWindowId())
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
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPath);
        return SaveToPathAsync(solution, currentPath);
    }

    public async Task<(bool success, string? path, string? error)> SaveAsAsync(Solution solution)
    {
        var picker = new FileSavePicker(_window.RequireWindowId())
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
                        var dialog = new ContentDialog
                {
                    Title = "Unsaved Changes",
                    Content = "You have unsaved changes in the current solution. Do you want to save before creating a new solution?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = _window.RequireXamlRoot()
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
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
        {
            return $"File does not appear to be valid JSON: {filePath}";
        }

        return null;
    }
}
