// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

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

    public RestApiClientRegistrationService(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient(MobiHttpClientNames.LanHealth);
        ClientId = GetOrCreateClientId();
    }

    /// <inheritdoc />
    public string ClientId { get; }

    /// <inheritdoc />
    public async Task<bool> RegisterAsync(string serverIp, int serverPort)
    {
        var clientId = ClientId;
        var deviceName = DeviceInfo.Current.Name;
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            deviceName = DeviceNameDefault;
        }

        var url = $"http://{serverIp}:{serverPort}/api/clients/register";
        var body = JsonSerializer.Serialize(new { clientId, deviceName });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetOrCreateClientId()
    {
        var existing = Preferences.Default.Get(ClientIdKey, string.Empty);
        if (!string.IsNullOrEmpty(existing))
        {
            return existing;
        }

        var id = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(ClientIdKey, id);
        return id;
    }
}
