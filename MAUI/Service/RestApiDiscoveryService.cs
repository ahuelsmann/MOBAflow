// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Microsoft.Extensions.Logging;
using Common.Configuration;

/// <summary>
/// REST-API Server Connection Service for MAUI.
/// Returns manually configured server IP and Port from settings.
/// No automatic discovery - user must configure IP address manually (like Z21).
/// </summary>
public class RestApiDiscoveryService
{
    private readonly ILogger<RestApiDiscoveryService> _logger;
    private readonly AppSettings _appSettings;

    public RestApiDiscoveryService(ILogger<RestApiDiscoveryService> logger, AppSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Gets the REST-API server endpoint from manual configuration.
    /// </summary>
    /// <returns>Server IP and Port from settings, or null if not configured</returns>
    public Task<(string? ip, int? port)> GetServerEndpointAsync()
    {
        // Use manual IP configuration (required)
        if (!string.IsNullOrWhiteSpace(_appSettings.RestApi.CurrentIpAddress))
        {
            _logger.LogInformation("✅ Using configured REST-API server: {Ip}:{Port}", 
                _appSettings.RestApi.CurrentIpAddress, _appSettings.RestApi.Port);
            return Task.FromResult<(string?, int?)>((_appSettings.RestApi.CurrentIpAddress, _appSettings.RestApi.Port));
        }

        // No IP configured
        _logger.LogWarning("⚠️ No REST-API server configured.");
        _logger.LogInformation("💡 Please enter the WebApp server IP address in settings (e.g., 192.168.0.78)");
        return Task.FromResult<(string?, int?)>((null, null));
    }
}
