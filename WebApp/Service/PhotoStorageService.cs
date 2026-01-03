// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Service;

using Microsoft.Extensions.Logging;
using System.IO;

/// <summary>
/// Service for managing photo storage on the server.
/// Photos are stored in wwwroot/photos/{category}/{entityId}.{ext}
/// </summary>
public class PhotoStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PhotoStorageService> _logger;

    public PhotoStorageService(IWebHostEnvironment environment, ILogger<PhotoStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Saves a photo from an uploaded stream.
    /// </summary>
    /// <param name="stream">Photo file stream</param>
    /// <param name="category">Category (locomotives, passenger-wagons, goods-wagons)</param>
    /// <param name="entityId">Entity GUID</param>
    /// <param name="fileExtension">File extension (e.g., .jpg)</param>
    /// <returns>Relative path to saved photo</returns>
    public async Task<string?> SavePhotoAsync(Stream stream, string category, Guid entityId, string fileExtension)
    {
        try
        {
            // Ensure photos directory exists
            var photosPath = Path.Combine(_environment.WebRootPath, "photos");
            Directory.CreateDirectory(photosPath);

            var categoryPath = Path.Combine(photosPath, category);
            Directory.CreateDirectory(categoryPath);

            // Save file
            var fileName = $"{entityId}{fileExtension}";
            var filePath = Path.Combine(categoryPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }

            _logger.LogInformation("Photo saved: {Category}/{FileName}", category, fileName);

            // Return relative path
            return $"photos/{category}/{fileName}";
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
    public bool DeletePhoto(string relativePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Photo deleted: {Path}", relativePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo: {Path}", relativePath);
            return false;
        }
    }
}
