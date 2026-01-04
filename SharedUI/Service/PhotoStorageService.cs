// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Microsoft.Extensions.Logging;

/// <summary>
/// Service for storing and managing photo files.
/// Works in both WebApp (ASP.NET Core) and WinUI (in-process Kestrel) contexts.
/// </summary>
public class PhotoStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<PhotoStorageService> _logger;

    private const string StorageFolder = "photos";

    public PhotoStorageService(ILogger<PhotoStorageService> logger)
    {
        _logger = logger;

        // Determine storage path based on platform
        _storagePath = GetStoragePath();

        // Create directory if not exists
        Directory.CreateDirectory(_storagePath);
        _logger.LogInformation("Photo storage initialized at: {StoragePath}", _storagePath);
    }

    /// <summary>
    /// Save a photo file and return the relative path for database storage.
    /// </summary>
    /// <param name="stream">Photo file stream</param>
    /// <param name="category">Category: locomotives, passenger-wagons, goods-wagons</param>
    /// <param name="entityId">Entity GUID</param>
    /// <param name="extension">File extension (e.g., ".jpg")</param>
    /// <returns>Relative path (e.g., "photos/locomotives/guid.jpg"), or null if save failed</returns>
    public async Task<string?> SavePhotoAsync(Stream stream, string category, Guid entityId, string extension)
    {
        try
        {
            // Create category subfolder
            var categoryFolder = Path.Combine(_storagePath, category);
            Directory.CreateDirectory(categoryFolder);

            // Generate filename
            var filename = $"{entityId}{extension}";
            var filePath = Path.Combine(categoryFolder, filename);

            // Save file
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream);

            // Return relative path for database
            var relativePath = Path.Combine(StorageFolder, category, filename).Replace("\\", "/");
            _logger.LogInformation("Photo saved: {RelativePath}", relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save photo: {Category}/{EntityId}", category, entityId);
            return null;
        }
    }

    /// <summary>
    /// Delete a photo file by relative path.
    /// </summary>
    public bool DeletePhoto(string relativePath)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, relativePath.Replace("/", "\\"));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Photo deleted: {RelativePath}", relativePath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete photo: {RelativePath}", relativePath);
            return false;
        }
    }

    /// <summary>
    /// Check if a photo exists.
    /// </summary>
    public bool PhotoExists(string relativePath)
    {
        var filePath = Path.Combine(_storagePath, relativePath.Replace("/", "\\"));
        return File.Exists(filePath);
    }

    /// <summary>
    /// Get absolute file path from relative path.
    /// </summary>
    public string GetAbsolutePath(string relativePath)
    {
        return Path.Combine(_storagePath, relativePath.Replace("/", "\\"));
    }

    /// <summary>
    /// Determine storage path based on current platform.
    /// </summary>
    private static string GetStoragePath()
    {
        // Check if running in ASP.NET Core context (WebApp)
        var contentRootPath = Environment.GetEnvironmentVariable("ASPNETCORE_CONTENTROOT");
        if (!string.IsNullOrEmpty(contentRootPath))
        {
            return Path.Combine(contentRootPath, StorageFolder);
        }

        // Check if running in WinUI context
        if (OperatingSystem.IsWindows())
        {
            // Store in user's AppData\Local\MOBAflow\photos
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "MOBAflow", StorageFolder);
        }

        // Fallback to current directory
        return Path.Combine(AppContext.BaseDirectory, StorageFolder);
    }
}
