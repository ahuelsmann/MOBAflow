// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Microsoft.Extensions.Logging;
using SharedUI.Interface;
using System.Text;
using System.Text.Json;

/// <summary>
/// Registers this MAUI app with the WinUI REST API so it appears in Overview "Connected clients".
/// </summary>
public sealed class RestApiClientRegistrationService : IRestApiClientRegistration
{
    private const string ClientIdKey = "MOBAflow.RestApi.ClientId";
    private const string DeviceNameDefault = "MOBAsmart";

    private readonly HttpClient _httpClient;
    private readonly ILogger<RestApiClientRegistrationService> _logger;

    public RestApiClientRegistrationService(HttpClient httpClient, ILogger<RestApiClientRegistrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> RegisterAsync(string serverIp, int serverPort)
    {
        var clientId = GetOrCreateClientId();
        var deviceName = DeviceInfo.Current.Name ?? DeviceNameDefault;
        if (string.IsNullOrWhiteSpace(deviceName))
            deviceName = DeviceNameDefault;

        var url = $"http://{serverIp}:{serverPort}/api/clients/register";
        var body = JsonSerializer.Serialize(new { clientId, deviceName });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Registered with REST API as {DeviceName} ({ClientId})", deviceName, clientId);
                return true;
            }
            _logger.LogWarning("REST API client registration failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "REST API client registration failed");
            return false;
        }
    }

    private static string GetOrCreateClientId()
    {
        var existing = Preferences.Default.Get(ClientIdKey, string.Empty);
        if (!string.IsNullOrEmpty(existing))
            return existing;
        var id = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(ClientIdKey, id);
        return id;
    }
}
