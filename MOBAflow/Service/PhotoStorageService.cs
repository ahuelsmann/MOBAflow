// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Path;

using SharedUI.Interface;

/// <summary>
/// Stores vehicle photos below the configured photo root.
/// </summary>
internal sealed class PhotoStorageService(AppSettings appSettings) : IPhotoStorageService
{
    private readonly AppSettings _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

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
            // The destination must stay under the photo storage root; unknown categories are rejected.
            if (!PhotoPathHelper.TryBuildPhotoUploadFullPath(
                    GetPhotoBaseDir(),
                    category,
                    entityId,
                    Path.GetExtension(sourceFilePath),
                    out var destinationPath,
                    out var relativePath))
            {
                return null;
            }

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
}
