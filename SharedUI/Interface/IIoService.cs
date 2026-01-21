// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Domain;

/// <summary>
/// Service interface for file I/O operations across platforms.
/// Provides platform-agnostic file loading, saving, and browsing functionality.
/// Implementations handle platform-specific file pickers and storage APIs.
/// </summary>
public interface IIoService
{
    /// <summary>
    /// Creates a new empty solution and updates the DI singleton.
    /// Prompts user for confirmation if unsaved changes exist.
    /// </summary>
    /// <param name="hasUnsavedChanges">Whether current solution has unsaved changes.</param>
    /// <returns>
    /// Tuple containing:
    /// - success: True if new solution was created successfully.
    /// - userCancelled: True if user cancelled the operation (e.g., chose not to discard changes).
    /// - error: Error message if operation failed, null otherwise.
    /// </returns>
    Task<(bool success, bool userCancelled, string? error)> NewSolutionAsync(bool hasUnsavedChanges);

    /// <summary>
    /// Opens a file picker and loads a solution from the selected JSON file.
    /// Updates LastSolutionPath in settings on success.
    /// </summary>
    /// <returns>
    /// Tuple containing:
    /// - solution: Deserialized Solution object, or null if cancelled/failed.
    /// - path: Full path to the loaded file, or null if cancelled.
    /// - error: Error message if loading failed, null otherwise.
    /// </returns>
    Task<(Solution? solution, string? path, string? error)> LoadAsync();

    /// <summary>
    /// Loads a solution from a specific file path without showing a file picker.
    /// Used for auto-loading last solution or programmatic loading.
    /// </summary>
    /// <param name="filePath">Full path to the solution JSON file.</param>
    /// <returns>
    /// Tuple containing:
    /// - solution: Deserialized Solution object, or null if failed.
    /// - path: Same as input filePath if successful, null otherwise.
    /// - error: Error message if loading failed, null otherwise.
    /// </returns>
    Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath);

    /// <summary>
    /// Saves the solution to a JSON file. Shows file picker if currentPath is null.
    /// Uses atomic write (temp file + rename) to prevent data corruption.
    /// </summary>
    /// <param name="solution">Solution object to serialize and save.</param>
    /// <param name="currentPath">Existing file path for overwrite, or null to show file picker.</param>
    /// <returns>
    /// Tuple containing:
    /// - success: True if save completed successfully.
    /// - path: Full path where file was saved, or null if cancelled/failed.
    /// - error: Error message if save failed, null otherwise.
    /// </returns>
    Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath);

    /// <summary>
    /// Opens a file picker to browse for a JSON file.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForJsonFileAsync();

    /// <summary>
    /// Opens a file picker to browse for an XML file (e.g., AnyRail layout).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForXmlFileAsync();

    /// <summary>
    /// Opens a file save picker for saving an XML file.
    /// </summary>
    /// <param name="suggestedFileName">Suggested file name</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> SaveXmlFileAsync(string suggestedFileName);

    /// <summary>
    /// Opens a file picker to browse for an audio file (WAV, MP3, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForAudioFileAsync();

    /// <summary>
    /// Opens a file picker to browse for a photo/image file (JPG, PNG, etc.).
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> BrowseForPhotoAsync();

    /// <summary>
    /// Saves a photo file to the application's local photos storage.
    /// </summary>
    /// <param name="sourceFilePath">Source photo file path</param>
    /// <param name="category">Photo category (e.g., "locomotives", "passenger-wagons", "goods-wagons")</param>
    /// <param name="entityId">Entity ID for filename</param>
    /// <returns>Relative path to saved photo (e.g., "photos/locomotives/{id}.jpg")</returns>
    Task<string?> SavePhotoAsync(string sourceFilePath, string category, Guid entityId);

    /// <summary>
    /// Converts a relative photo path to an absolute file system path.
    /// </summary>
    /// <param name="relativePath">Relative path (e.g., returned from SavePhotoAsync)</param>
    /// <returns>Absolute file system path, or null if path is invalid</returns>
    string? GetPhotoFullPath(string? relativePath);
}
