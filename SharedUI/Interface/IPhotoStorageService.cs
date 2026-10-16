// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>Stores vehicle photos below the configured photo root and resolves their paths.</summary>
public interface IPhotoStorageService
{
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
