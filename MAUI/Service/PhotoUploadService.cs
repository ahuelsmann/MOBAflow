// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;

/// <summary>
/// Service for uploading photos to MOBAflow WebApp REST-API.
/// </summary>
public class PhotoUploadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PhotoUploadService> _logger;

    public PhotoUploadService(HttpClient httpClient, ILogger<PhotoUploadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a photo to the server.
    /// </summary>
    /// <param name="serverIp">Server IP address</param>
    /// <param name="serverPort">Server port</param>
    /// <param name="photoPath">Local photo file path</param>
    /// <param name="category">Category (locomotives, passenger-wagons, goods-wagons)</param>
    /// <param name="entityId">Entity GUID</param>
    /// <returns>Success status and server photo path</returns>
    public async Task<(bool success, string? photoPath, string? error)> UploadPhotoAsync(
        string serverIp,
        int serverPort,
        string photoPath,
        string category,
        Guid entityId)
    {
        try
        {
            if (!File.Exists(photoPath))
                return (false, null, "Photo file not found");

            var url = $"http://{serverIp}:{serverPort}/api/photos/upload";

            using var form = new MultipartFormDataContent();

            // Add photo file
            var fileStream = File.OpenRead(photoPath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            var fileName = Path.GetFileName(photoPath);
            form.Add(streamContent, "file", fileName);

            // Add metadata
            form.Add(new StringContent(category), "category");
            form.Add(new StringContent(entityId.ToString()), "entityId");

            _logger.LogInformation("Uploading photo to {Url}: {Category}/{EntityId}", url, category, entityId);

            var response = await _httpClient.PostAsync(url, form);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Photo uploaded successfully: {Result}", result);

                // Parse JSON response (simple approach - could use System.Text.Json for robustness)
                // Expected: {"success":true,"photoPath":"photos/locomotives/xxx.jpg","message":"..."}
                var pathStart = result.IndexOf("\"photoPath\":\"");
                if (pathStart > 0)
                {
                    pathStart += "\"photoPath\":\"".Length;
                    var pathEnd = result.IndexOf("\"", pathStart);
                    if (pathEnd > pathStart)
                    {
                        var serverPhotoPath = result.Substring(pathStart, pathEnd - pathStart);
                        return (true, serverPhotoPath, null);
                    }
                }

                return (true, null, null);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Photo upload failed: {StatusCode} - {Error}", response.StatusCode, error);
                return (false, null, $"Upload failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo");
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Health check to verify server is reachable.
    /// </summary>
    public async Task<bool> HealthCheckAsync(string serverIp, int serverPort)
    {
        try
        {
            var url = $"http://{serverIp}:{serverPort}/api/photos/health";
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
