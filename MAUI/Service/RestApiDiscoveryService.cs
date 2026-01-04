// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Android.OS;
using Common.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// REST-API Server Connection Service for MAUI.
/// Returns manually configured server IP and Port from settings.
/// Automatically uses 10.0.2.2 when running on Android emulator to access host machine.
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
    /// Automatically handles Android emulator special addressing (10.0.2.2 for localhost).
    /// </summary>
    /// <returns>Server IP and Port from settings, or null if not configured</returns>
    public Task<(string? ip, int? port)> GetServerEndpointAsync()
    {
        // Use manual IP configuration (required)
        if (!string.IsNullOrWhiteSpace(_appSettings.RestApi.CurrentIpAddress))
        {
            var configuredIp = _appSettings.RestApi.CurrentIpAddress;
            var effectiveIp = GetEffectiveIpAddress(configuredIp);
            
            if (effectiveIp != configuredIp)
            {
                _logger.LogInformation("🔄 Android emulator detected - using {EmulatorIp} instead of {ConfiguredIp}", 
                    effectiveIp, configuredIp);
            }
            
            _logger.LogInformation("✅ Using REST-API server: {Ip}:{Port}", 
                effectiveIp, _appSettings.RestApi.Port);
            return Task.FromResult<(string?, int?)>((effectiveIp, _appSettings.RestApi.Port));
        }

        // No IP configured
        _logger.LogWarning("⚠️ No REST-API server configured.");
        _logger.LogInformation("💡 Configuration required:");
        _logger.LogInformation("   • For Android Emulator: Use '10.0.2.2' to access host PC");
        _logger.LogInformation("   • For Physical Device: Enter your PC's IP (e.g., 192.168.0.79)");
        _logger.LogInformation("   • Server must be running on port {Port}", _appSettings.RestApi.Port);
        return Task.FromResult<(string?, int?)>((null, null));
    }

    /// <summary>
    /// Detects if running on Android emulator and returns appropriate IP address.
    /// Android emulator uses 10.0.2.2 to access the host machine's localhost.
    /// </summary>
    private string GetEffectiveIpAddress(string configuredIp)
    {
        // Check if running on Android platform
#if ANDROID
        // Detect if running in Android emulator
        if (IsAndroidEmulator())
        {
            // Android emulator uses 10.0.2.2 to access host's localhost
            // Convert any localhost/127.0.0.1 or private IP to emulator special IP
            return "10.0.2.2";
        }
#endif
        // Physical device or other platforms - use configured IP as-is
        return configuredIp;
    }

#if ANDROID
    /// <summary>
    /// Detects if the app is running on an Android emulator.
    /// </summary>
    private bool IsAndroidEmulator()
    {
        try
        {
            // Check for common emulator indicators
            var brand = Build.Brand?.ToLowerInvariant() ?? string.Empty;
            var device = Build.Device?.ToLowerInvariant() ?? string.Empty;
            var model = Build.Model?.ToLowerInvariant() ?? string.Empty;
            var product = Build.Product?.ToLowerInvariant() ?? string.Empty;
            var hardware = Build.Hardware?.ToLowerInvariant() ?? string.Empty;

            // Common emulator signatures
            return brand.Contains("generic") || 
                   device.Contains("generic") ||
                   model.Contains("emulator") || 
                   model.Contains("sdk") ||
                   product.Contains("sdk") || 
                   product.Contains("emulator") ||
                   hardware.Contains("goldfish") || 
                   hardware.Contains("ranchu");
        }
        catch
        {
            // If detection fails, assume physical device for safety
            return false;
        }
    }
#endif
}
