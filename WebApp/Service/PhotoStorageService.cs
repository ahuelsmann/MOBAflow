// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Service;

using Microsoft.Extensions.Logging;
using System.IO;

/// <summary>
/// Service for managing photo storage.
/// Photos are stored in %LOCALAPPDATA%\MOBAflow\photos\{category}\{entityId}.{ext}
/// Same location as WinUI for consistency and file sharing.
/// </summary>
public class PhotoStorageService
{
    private readonly ILogger<PhotoStorageService> _logger;
    private readonly string _photosBasePath;

    public PhotoStorageService(ILogger<PhotoStorageService> logger)
    {
        _logger = logger;
        
        // Use same location as WinUI: %LOCALAPPDATA%\MOBAflow\photos
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _photosBasePath = Path.Combine(localAppData, "MOBAflow", "photos");
        
        // Ensure base directory exists
        Directory.CreateDirectory(_photosBasePath);
    }

    /// <summary>
    /// Saves a photo from an uploaded stream.
    /// </summary>
    /// <param name="stream">Photo file stream</param>
    /// <param name="category">Category (locomotives, passenger-wagons, goods-wagons)</param>
    /// <param name="entityId">Entity GUID</param>
    /// <param name="fileExtension">File extension (e.g., .jpg)</param>
    /// <returns>Absolute path to saved photo</returns>
    public async Task<string?> SavePhotoAsync(Stream stream, string category, Guid entityId, string fileExtension)
    {
        try
        {
            // Ensure category directory exists
            var categoryPath = Path.Combine(_photosBasePath, category);
            Directory.CreateDirectory(categoryPath);

            // Save file
            var fileName = $"{entityId}{fileExtension}";
            var filePath = Path.Combine(categoryPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }

            _logger.LogInformation("Photo saved: {FilePath}", filePath);

            // Return absolute path (same format as WinUI)
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving photo for {Category}/{EntityId}", category, entityId);
            return null;
        }
    }

    /// <summary>
    /// Deletes a photo if it exists.
    /// </summary>
    public bool DeletePhoto(string photoPath)
    {
        try
        {
            if (File.Exists(photoPath))
            {
                File.Delete(photoPath);
                _logger.LogInformation("Photo deleted: {Path}", photoPath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo: {Path}", photoPath);
            return false;
        }
    }
}

