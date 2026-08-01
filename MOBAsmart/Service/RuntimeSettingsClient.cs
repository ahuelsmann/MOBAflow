// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Security;

using SharedUI.Interface;

using System.Text.Json;

/// <summary>
/// Fetches runtime settings (Z21 endpoint) that MOBAflow pushed to MOBApi.
/// </summary>
public sealed class RuntimeSettingsClient : IRuntimeSettingsClient
{
    private readonly IRemoteControlAuthenticatedHttpClient _httpClient;

    public RuntimeSettingsClient(IRemoteControlAuthenticatedHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<(string? ip, int? port)> GetZ21EndpointAsync(
        string serverIp,
        int serverPort,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serverIp) || serverPort <= 0)
        {
            return (null, null);
        }

        try
        {
            using var response = await _httpClient
                .GetAsync("api/runtime-settings", cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return (null, null);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;
            if (!root.TryGetProperty("z21IpAddress", out var ipElement))
            {
                return (null, null);
            }

            var ip = ipElement.GetString()?.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                return (null, null);
            }

            var port = 21105;
            if (root.TryGetProperty("z21Port", out var portElement) && portElement.TryGetInt32(out var parsedPort) && parsedPort > 0)
            {
                port = parsedPort;
            }

            return (ip, port);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }
}