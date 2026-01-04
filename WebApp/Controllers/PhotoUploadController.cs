// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Controllers;

using Microsoft.AspNetCore.Mvc;
using Service;

/// <summary>
/// REST API Controller for photo uploads from MAUI clients.
/// </summary>
[ApiController]
[Route("api/photos")]
public class PhotoUploadController : ControllerBase
{
    private readonly PhotoStorageService _photoStorage;
    private readonly ILogger<PhotoUploadController> _logger;

    private static readonly HashSet<string> AllowedExtensions = new()
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public PhotoUploadController(PhotoStorageService photoStorage, ILogger<PhotoUploadController> logger)
    {
        _photoStorage = photoStorage;
        _logger = logger;
    }

    /// <summary>
    /// Upload a photo for a locomotive, passenger wagon, or goods wagon.
    /// POST /api/photos/upload
    /// </summary>
    /// <param name="file">Photo file (multipart/form-data)</param>
    /// <param name="category">Category: locomotives, passenger-wagons, goods-wagons</param>
    /// <param name="entityId">Entity GUID</param>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadPhoto(
        [FromForm] IFormFile file,
        [FromForm] string category,
        [FromForm] Guid entityId)
    {
        try
        {
            // Validation
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            if (file.Length > MaxFileSize)
                return BadRequest(new { error = $"File size exceeds {MaxFileSize / 1024 / 1024} MB limit" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest(new { error = $"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}" });

            var validCategories = new[] { "locomotives", "passenger-wagons", "goods-wagons" };
            if (!validCategories.Contains(category))
                return BadRequest(new { error = $"Invalid category. Allowed: {string.Join(", ", validCategories)}" });

            // Save photo
            using var stream = file.OpenReadStream();
            var relativePath = await _photoStorage.SavePhotoAsync(stream, category, entityId, extension);

            if (relativePath == null)
                return StatusCode(500, new { error = "Failed to save photo" });

            _logger.LogInformation("Photo uploaded: {Category}/{EntityId} by {RemoteIp}", 
                category, entityId, HttpContext.Connection.RemoteIpAddress);

            return Ok(new
            {
                success = true,
                photoPath = relativePath,
                message = "Photo uploaded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check endpoint.
    /// GET /api/photos/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "MOBAflow Photo Upload API",
            status = "healthy",
            version = "1.0.0"
        });
    }
}
