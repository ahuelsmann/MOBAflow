// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moba.SharedUI.Service;
using Hubs;


[ApiController]
[Route("api/photos")]
public class PhotoUploadController : ControllerBase
{
    private readonly PhotoStorageService _photoStorage;
    private readonly IHubContext<PhotoHub> _hubContext;
    private readonly ILogger<PhotoUploadController> _logger;

    private static readonly HashSet<string> AllowedExtensions = new() { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024;

    public PhotoUploadController(
        PhotoStorageService photoStorage,
        IHubContext<PhotoHub> hubContext,
        ILogger<PhotoUploadController> logger)
    {
        _photoStorage = photoStorage;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadPhoto(
        [FromForm] IFormFile file,
        [FromForm] string category,
        [FromForm] Guid entityId)
    {
        if (file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new { error = $"Invalid type. Allowed: {string.Join(", ", AllowedExtensions)}" });

        // ✅ Category "latest" means: assign to currently selected item in WinUI
        // Generate new GUID for the photo filename
        var photoGuid = Guid.NewGuid();
        
        using var stream = file.OpenReadStream();
        var relativePath = await _photoStorage.SavePhotoAsync(stream, "temp", photoGuid, extension);

        if (relativePath == null)
            return StatusCode(500, new { error = "Failed to save photo" });

        _logger.LogInformation("📸 Photo uploaded: {Category}/{EntityId} → {Path}", category, entityId, relativePath);

        // ✅ Notify all connected SignalR clients (WinUI will handle assignment)
        try
        {
            await _hubContext.Clients.All.SendAsync("PhotoUploaded", relativePath, DateTime.UtcNow);
            _logger.LogInformation("✅ SignalR notification sent: {Path}", relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR notification");
        }

        return Ok(new { success = true, photoPath = relativePath });
    }

    [HttpGet("health")]
    public IActionResult HealthCheck() => Ok(new { service = "MOBAflow Photo API (WinUI)", status = "healthy", version = "1.0.0" });
}
