// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Domain;
using Interface;

/// <summary>
/// Null Object implementation of IIoService for platforms that don't support file operations (WebApp, MAUI).
/// All operations return "not supported" errors or no-op results.
/// </summary>
public class NullIoService : IIoService
{
    /// <summary>
    /// Creates a new instance of the <see cref="NullIoService"/> class.
    /// </summary>
    public NullIoService()
    {
    }

    /// <summary>
    /// Always reports that creating a new solution is not supported on this platform.
    /// </summary>
    /// <param name="hasUnsavedChanges">Indicates whether there are unsaved changes in the current solution.</param>
    /// <returns>
    /// A task that always returns <c>false</c> for success, <c>false</c> for userCancelled and an explanatory error message.
    /// </returns>
    public Task<(bool success, bool userCancelled, string? error)> NewSolutionAsync(bool hasUnsavedChanges)
    {
        return Task.FromResult<(bool, bool, string?)>((false, false, "File operations not supported on this platform"));
    }

    /// <summary>
    /// Always returns that loading a solution is not supported on this platform.
    /// </summary>
    /// <returns>
    /// A task that always returns <c>null</c> for solution and path and an explanatory error message.
    /// </returns>
    public Task<(Solution? solution, string? path, string? error)> LoadAsync()
    {
        return Task.FromResult<(Solution?, string?, string?)>((null, null, "File operations not supported on this platform"));
    }

    /// <summary>
    /// Always returns that loading a solution from a specified path is not supported on this platform.
    /// </summary>
    /// <param name="filePath">The file path that would normally be loaded.</param>
    /// <returns>
    /// A task that always returns <c>null</c> for solution and path and an explanatory error message.
    /// </returns>
    public Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath)
    {
        return Task.FromResult<(Solution?, string?, string?)>((null, null, "File operations not supported on this platform"));
    }

    /// <summary>
    /// Always reports that saving a solution is not supported on this platform.
    /// </summary>
    /// <param name="solution">The solution that would normally be saved.</param>
    /// <param name="currentPath">The current file path of the solution, if any.</param>
    /// <returns>
    /// A task that always returns <c>false</c> for success, <c>null</c> for path and an explanatory error message.
    /// </returns>
    public Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath)
    {
        return Task.FromResult<(bool, string?, string?)>((false, null, "File operations not supported on this platform"));
    }

    /// <summary>
    /// Always returns <c>null</c> because browsing for JSON files is not supported on this platform.
    /// </summary>
    public Task<string?> BrowseForJsonFileAsync()
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Always returns <c>null</c> because browsing for XML files is not supported on this platform.
    /// </summary>
    public Task<string?> BrowseForXmlFileAsync()
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Always returns <c>null</c> because saving XML files is not supported on this platform.
    /// </summary>
    /// <param name="suggestedFileName">The file name that would normally be suggested.</param>
    public Task<string?> SaveXmlFileAsync(string suggestedFileName)
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Always returns <c>null</c> because browsing for audio files is not supported on this platform.
    /// </summary>
    public Task<string?> BrowseForAudioFileAsync()
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Always returns <c>null</c> because browsing for photo files is not supported on this platform.
    /// </summary>
    public Task<string?> BrowseForPhotoAsync()
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Always returns <c>null</c> because saving photo files is not supported on this platform.
    /// </summary>
    /// <param name="sourceFilePath">The original photo file path.</param>
    /// <param name="category">The logical category (for example, locomotives).</param>
    /// <param name="entityId">The associated domain entity identifier.</param>
    public Task<string?> SavePhotoAsync(string sourceFilePath, string category, Guid entityId)
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Always returns <c>null</c> because this implementation does not map photo paths.
    /// </summary>
    /// <param name="relativePath">The relative photo path from the database.</param>
    /// <returns>Always <c>null</c>.</returns>
    public string? GetPhotoFullPath(string? relativePath)
    {
        return null;
    }
}
