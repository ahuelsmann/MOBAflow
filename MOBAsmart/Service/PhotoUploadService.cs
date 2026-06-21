// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Discovery;

using SharedUI.Interface;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;

/// <summary>
/// Service for uploading photos to MOBAflow WebApp REST-API.
/// </summary>
public class PhotoUploadService : IPhotoUploadService
{
    /// <summary>
    /// LAN health checks bypass the platform <see cref="HttpClient"/> handler so Android does not route
    /// private IPs through a system proxy/VPN (avoids <c>SocksSocketImpl</c> / failed connects in logs).
    /// </summary>
    private readonly HttpClient _httpClient;
    private readonly HttpClient _lanHealthHttpClient;

    public PhotoUploadService(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient(MobiHttpClientNames.Platform);
        _lanHealthHttpClient = httpClientFactory.CreateClient(MobiHttpClientNames.LanHealth);
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
        StreamContent? streamContent = null;
        Stream? fileStream = null;

        try
        {
            if (!File.Exists(photoPath))
                return (false, null, "Photo file not found");

            // Test network connectivity first
            var isReachable = await TestConnectivityAsync(serverIp, serverPort);
            if (!isReachable)
            {
                var errorMsg = BuildConnectivityErrorMessage(serverIp, serverPort);
                return (false, null, errorMsg);
            }

            var url = $"http://{serverIp}:{serverPort}/api/photos/upload";

            using var form = new MultipartFormDataContent();

            // Add photo file
            fileStream = File.OpenRead(photoPath);
            streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            var fileName = Path.GetFileName(photoPath);
            form.Add(streamContent, "file", fileName);

            // Add metadata
            form.Add(new StringContent(category), "category");
            form.Add(new StringContent(entityId.ToString()), "entityId");

            var response = await _httpClient.PostAsync(url, form);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                // Parse JSON response (simple approach - could use System.Text.Json for robustness)
                // Expected: {"success":true,"photoPath":"photos/locomotives/xxx.jpg","message":"..."}
                var pathStart = result.IndexOf("\"photoPath\":\"", StringComparison.Ordinal);
                if (pathStart > 0)
                {
                    pathStart += "\"photoPath\":\"".Length;
                    var pathEnd = result.IndexOf("\"", pathStart, StringComparison.Ordinal);
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
                _ = await response.Content.ReadAsStringAsync();
                return (false, null, $"Upload failed: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"Connection failed: {ex.Message}\n\nTroubleshooting:\n" +
                          $"• Verify server IP: {serverIp}\n" +
                          $"• Check server is running on port {serverPort}\n" +
                          $"• Ensure device and server are on same network";
            return (false, null, errorMsg);
        }
        catch (TaskCanceledException)
        {
            return (false, null, "Upload timeout - file may be too large or connection too slow");
        }
        catch (Exception ex)
        {
            return (false, null, $"Upload error: {ex.Message}");
        }
        finally
        {
            // Proper cleanup
            streamContent?.Dispose();
            fileStream?.Dispose();
        }
    }

    /// <summary>
    /// Tests if server is reachable via TCP connection.
    /// </summary>
    private async Task<bool> TestConnectivityAsync(string serverIp, int serverPort)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(serverIp, serverPort);
            var timeoutTask = Task.Delay(3000); // 3 second timeout

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return false;
            }

            if (tcpClient.Connected)
            {
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Health check to verify server is reachable.
    /// </summary>
    public async Task<bool> HealthCheckAsync(string serverIp, int serverPort)
    {
        try
        {
            var url = $"http://{serverIp}:{serverPort}{MobApiHealthProbe.HealthPath}";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var response = await _lanHealthHttpClient
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            return MobApiHealthProbe.IsHealthyResponse(body);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds a user-friendly error message when server cannot be reached (smartphone on LAN to PC running MOBAflow).
    /// </summary>
    private string BuildConnectivityErrorMessage(string serverIp, int serverPort)
    {
        var message = $"Cannot reach server at {serverIp}:{serverPort}\n\n";

#if ANDROID
        message += "📱 On your phone:\n";
        message += "   • Use the same Wi‑Fi as the PC running MOBAflow\n";
        message += $"   • Verify the PC’s LAN address ({serverIp}) and REST port\n";
        message += "   • Allow inbound TCP on that port in Windows Firewall\n\n";
#else
        message += "Troubleshooting:\n";
#endif

        message += "✓ MOBAflow is running on the PC with REST API started\n";
        message += $"✓ REST API listens on port {serverPort}\n";
        message += $"✓ Windows Firewall allows port {serverPort}";

        return message;
    }
}