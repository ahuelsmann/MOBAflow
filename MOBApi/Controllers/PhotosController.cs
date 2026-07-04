// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Controllers;

using Common.Path;
using Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// REST API for photo health check and upload (MAUI compatibility).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PhotosController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private readonly IHubContext<PhotoHub>? _hubContext;
    public PhotosController(IHubContext<PhotoHub>? hubContext = null)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Health check endpoint for MAUI app. Returns OK when REST API is running.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { service = "MOBAflow MOBAapi", status = "healthy", version = "1.0.0" });
    }

    /// <summary>
    /// Serves a photo file by relative storage path (e.g. photos/locomotives/{id}.jpg).
    /// </summary>
    [HttpGet("file")]
    public IActionResult GetFile([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(new { error = "Path is required" });
        }

        var normalized = PhotoPathHelper.NormalizeStoredRelativePath(path);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return BadRequest(new { error = "Invalid path" });
        }

        var fullPath = PhotoPathHelper.TryResolveExistingPhotoFullPath(
            Environment.GetEnvironmentVariable("MOBAFLOW_PHOTOS_PATH"),
            normalized);
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return NotFound();
        }

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return PhysicalFile(fullPath, contentType);
    }

    /// <summary>
    /// Upload a photo (e.g. from MAUI). Saves to MOBAflow Photos folder under My Documents.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile? file,
        [FromForm] string? category,
        [FromForm] Guid entityId,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new { error = $"Invalid type. Allowed: {string.Join(", ", AllowedExtensions)}" });
        var baseDir = GetPhotoBaseDir();
        if (!PhotoPathHelper.TryBuildPhotoUploadFullPath(
                baseDir,
                category ?? string.Empty,
                entityId,
                extension,
                out var fullPath,
                out var relativePath)
            || string.IsNullOrWhiteSpace(fullPath)
            || string.IsNullOrWhiteSpace(relativePath))
        {
            return BadRequest(new { error = "Invalid photo category or path." });
        }

        var categoryDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(categoryDir))
        {
            Directory.CreateDirectory(categoryDir);
        }

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, cancellationToken);
        if (_hubContext != null)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("PhotoUploaded", relativePath, DateTime.UtcNow);
            }

            catch
            {
                // Ignore if no clients connected
            }
        }

        return Ok(new { success = true, photoPath = relativePath });
    }

    private static string GetPhotoBaseDir()
    {
        return PhotoPathHelper.ResolvePhotoBaseDirectory(Environment.GetEnvironmentVariable("MOBAFLOW_PHOTOS_PATH"));
    }
}