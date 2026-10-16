// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Domain;

/// <summary>
/// Creates, loads and saves solution files. Hosts without file access use a null implementation
/// whose <see cref="CanPersist"/> is <see langword="false"/>.
/// </summary>
public interface ISolutionIoService
{
    /// <summary>Whether this host can load and save solution files.</summary>
    bool CanPersist { get; }

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
    /// Saves the solution to an existing JSON file without showing a file picker.
    /// Uses atomic write (temp file + rename) to prevent data corruption.
    /// </summary>
    /// <param name="solution">Solution object to serialize and save.</param>
    /// <param name="currentPath">Existing file path for overwrite.</param>
    /// <returns>
    /// Tuple containing:
    /// - success: True if save completed successfully.
    /// - path: Full path where file was saved, or null if failed.
    /// - error: Error message if save failed, null otherwise.
    /// </returns>
    Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string currentPath);

    /// <summary>
    /// Shows a file picker and saves the solution to the selected JSON file.
    /// </summary>
    /// <param name="solution">Solution object to serialize and save.</param>
    /// <returns>
    /// Tuple containing:
    /// - success: True if save completed successfully.
    /// - path: Full path where file was saved, or null if cancelled/failed.
    /// - error: Error message if save failed, null otherwise.
    /// </returns>
    Task<(bool success, string? path, string? error)> SaveAsAsync(Solution solution);
}
