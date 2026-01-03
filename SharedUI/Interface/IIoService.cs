// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Domain;

public interface IIoService
{
    /// <summary>
    /// Creates a new empty solution and updates the DI singleton.
    /// Prompts user for confirmation if unsaved changes exist.
    /// </summary>
    /// <param name="hasUnsavedChanges">Whether current solution has unsaved changes</param>
    Task<(bool success, bool userCancelled, string? error)> NewSolutionAsync(bool hasUnsavedChanges);
    
    Task<(Solution? solution, string? path, string? error)> LoadAsync();
    Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath);
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
}
