// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.RestApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moba.RestApi.Hubs;

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
        return Ok(new { service = "MOBAflow REST API", status = "healthy", version = "1.0.0" });
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

        var cat = string.IsNullOrWhiteSpace(category) ? "photos" : category.Trim();
        var baseDir = GetPhotoBaseDir();
        var categoryDir = Path.Combine(baseDir, cat);
        Directory.CreateDirectory(categoryDir);

        var fileName = $"{entityId:N}{extension}";
        var fullPath = Path.Combine(categoryDir, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, cancellationToken);

        var relativePath = $"photos/{cat}/{fileName}";
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
        var configured = Environment.GetEnvironmentVariable("MOBAFLOW_PHOTOS_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MOBAflow", "Photos");
    }
}
