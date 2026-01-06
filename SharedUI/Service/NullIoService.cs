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
    public Task<(bool success, bool userCancelled, string? error)> NewSolutionAsync(bool hasUnsavedChanges)
    {
        return Task.FromResult<(bool, bool, string?)>((false, false, "File operations not supported on this platform"));
    }

    public Task<(Solution? solution, string? path, string? error)> LoadAsync()
    {
        return Task.FromResult<(Solution?, string?, string?)>((null, null, "File operations not supported on this platform"));
    }

    public Task<(Solution? solution, string? path, string? error)> LoadFromPathAsync(string filePath)
    {
        return Task.FromResult<(Solution?, string?, string?)>((null, null, "File operations not supported on this platform"));
    }

    public Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath)
    {
        return Task.FromResult<(bool, string?, string?)>((false, null, "File operations not supported on this platform"));
    }

    public Task<string?> BrowseForJsonFileAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> BrowseForXmlFileAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> SaveXmlFileAsync(string suggestedFileName)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> BrowseForAudioFileAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> BrowseForPhotoAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> SavePhotoAsync(string sourceFilePath, string category, Guid entityId)
    {
        return Task.FromResult<string?>(null);
    }

    public string? GetPhotoFullPath(string? relativePath)
    {
        return null;
    }
}
