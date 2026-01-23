// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Android.OS;

using Common.Configuration;

using Microsoft.Extensions.Logging;

using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// REST-API Server Discovery Service for MAUI.
/// Supports both automatic UDP Multicast discovery and manual IP configuration.
/// Automatically uses 10.0.2.2 when running on Android emulator to access host machine.
/// </summary>
public class RestApiDiscoveryService
{
    private const int DISCOVERY_PORT = 21106;
    private const string DISCOVERY_REQUEST = "MOBAFLOW_DISCOVER";
    private const string DISCOVERY_RESPONSE_PREFIX = "MOBAFLOW_REST_API";
    private const string MULTICAST_ADDRESS = "239.255.42.99";
    private const int DISCOVERY_TIMEOUT_MS = 3000;

    private readonly ILogger<RestApiDiscoveryService> _logger;
    private readonly AppSettings _appSettings;

    public RestApiDiscoveryService(ILogger<RestApiDiscoveryService> logger, AppSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Gets the REST-API server endpoint using auto-discovery or manual configuration.
    /// First attempts UDP Multicast discovery, then falls back to manual configuration.
    /// </summary>
    /// <returns>Server IP and Port, or null if not found</returns>
    public async Task<(string? ip, int? port)> GetServerEndpointAsync()
    {
        // On Android emulator, skip discovery and use 10.0.2.2 directly
        if (IsRunningOnEmulator())
        {
            _logger.LogInformation("🔄 Android emulator detected - using 10.0.2.2:{Port}", _appSettings.RestApi.Port);
            return ("10.0.2.2", _appSettings.RestApi.Port);
        }

        // Try auto-discovery first
        var discovered = await DiscoverServerAsync();
        if (discovered.ip != null)
        {
            return discovered;
        }

        // Fall back to manual configuration
        return GetManualConfiguration();
    }

    /// <summary>
    /// Attempts to discover the REST-API server via UDP Multicast.
    /// </summary>
    /// <returns>Server IP and Port if discovered, null otherwise</returns>
    public async Task<(string? ip, int? port)> DiscoverServerAsync()
    {
        _logger.LogInformation("🔍 Starting UDP Multicast discovery for MOBAflow server...");

        try
        {
            using var udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            // Set timeout for receive
            udpClient.Client.ReceiveTimeout = DISCOVERY_TIMEOUT_MS;

            // Send discovery request to multicast group
            var requestBytes = Encoding.UTF8.GetBytes(DISCOVERY_REQUEST);
            var multicastEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_ADDRESS), DISCOVERY_PORT);

            _logger.LogDebug("📤 Sending discovery request to {MulticastAddress}:{Port}",
                MULTICAST_ADDRESS, DISCOVERY_PORT);

            await udpClient.SendAsync(requestBytes, requestBytes.Length, multicastEndpoint);

            // Wait for response with timeout
            using var cts = new CancellationTokenSource(DISCOVERY_TIMEOUT_MS);

            try
            {
                var result = await udpClient.ReceiveAsync(cts.Token);
                var response = Encoding.UTF8.GetString(result.Buffer);

                _logger.LogDebug("📥 Received response: {Response}", response);

                // Parse response: "MOBAFLOW_REST_API|192.168.0.100|5001"
                if (response.StartsWith(DISCOVERY_RESPONSE_PREFIX))
                {
                    var parts = response.Split('|');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var port))
                    {
                        var ip = parts[1];
                        _logger.LogInformation("✅ Server discovered: {Ip}:{Port}", ip, port);
                        return (ip, port);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("⏱️ Discovery timeout - no response received");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                _logger.LogDebug("⏱️ Discovery timeout - no response received");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Discovery failed");
        }

        _logger.LogInformation("ℹ️ Auto-discovery did not find server - using manual configuration");
        return (null, null);
    }

    /// <summary>
    /// Gets the server endpoint from manual configuration.
    /// </summary>
    private (string? ip, int? port) GetManualConfiguration()
    {
        if (!string.IsNullOrWhiteSpace(_appSettings.RestApi.CurrentIpAddress))
        {
            var configuredIp = _appSettings.RestApi.CurrentIpAddress;
            _logger.LogInformation("✅ Using manually configured server: {Ip}:{Port}",
                configuredIp, _appSettings.RestApi.Port);
            return (configuredIp, _appSettings.RestApi.Port);
        }

        _logger.LogWarning("⚠️ No REST-API server configured.");
        _logger.LogInformation("💡 Configuration required:");
        _logger.LogInformation("   • For Android Emulator: Use '10.0.2.2' to access host PC");
        _logger.LogInformation("   • For Physical Device: Enter your PC's IP (e.g., 192.168.0.79)");
        _logger.LogInformation("   • Or enable Auto-Discovery if MOBAflow is running on the same network");
        return (null, null);
    }

    /// <summary>
    /// Checks if running on Android emulator.
    /// </summary>
    private bool IsRunningOnEmulator()
    {
#if ANDROID
        return IsAndroidEmulator();
#else
        return false;
#endif
    }

#if ANDROID
    /// <summary>
    /// Detects if the app is running on an Android emulator.
    /// </summary>
    private static bool IsAndroidEmulator()
    {
        try
        {
            var brand = Build.Brand?.ToLowerInvariant() ?? string.Empty;
            var device = Build.Device?.ToLowerInvariant() ?? string.Empty;
            var model = Build.Model?.ToLowerInvariant() ?? string.Empty;
            var product = Build.Product?.ToLowerInvariant() ?? string.Empty;
            var hardware = Build.Hardware?.ToLowerInvariant() ?? string.Empty;

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
            return false;
        }
    }
#endif
}
