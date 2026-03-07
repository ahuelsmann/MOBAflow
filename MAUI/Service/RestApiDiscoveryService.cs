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
/// Discovery only via UDP Multicast (no saved/manual IP).
/// Uses 10.0.2.2 on Android emulator to reach host PC.
/// </summary>
public class RestApiDiscoveryService
{
    private const int DiscoveryPort = 21106;
    private const string DiscoveryRequest = "MOBAFLOW_DISCOVER";
    private const string DiscoveryResponsePrefix = "MOBAFLOW_REST_API";
    private const string MulticastAddress = "239.255.42.99";
    private const int DiscoveryTimeoutMs = 3000;

    private readonly ILogger<RestApiDiscoveryService> _logger;
    private readonly AppSettings _appSettings;

    public RestApiDiscoveryService(ILogger<RestApiDiscoveryService> logger, AppSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Returns REST-API endpoint only when discovered via UDP multicast (or emulator).
    /// </summary>
    public async Task<(string? ip, int? port)> GetServerEndpointByDiscoveryOnlyAsync()
    {
        if (IsRunningOnEmulator())
        {
            _logger.LogInformation("🔄 Android emulator detected - using 10.0.2.2:{Port}", _appSettings.RestApi.Port);
            return ("10.0.2.2", _appSettings.RestApi.Port);
        }
        return await DiscoverServerAsync();
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
#if ANDROID
            AcquireMulticastLock();
#endif
            using var udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            // Bind before send on all platforms so we have a local port to receive the unicast response
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            // Set timeout for receive
            udpClient.Client.ReceiveTimeout = DiscoveryTimeoutMs;

            // Send discovery request to multicast group
            var requestBytes = Encoding.UTF8.GetBytes(DiscoveryRequest);
            var multicastEndpoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), DiscoveryPort);

            _logger.LogDebug("📤 Sending discovery request to {MulticastAddress}:{Port}",
                MulticastAddress, DiscoveryPort);

            await udpClient.SendAsync(requestBytes, requestBytes.Length, multicastEndpoint);

            // Wait for response with timeout
            using var cts = new CancellationTokenSource(DiscoveryTimeoutMs);

            try
            {
                var result = await udpClient.ReceiveAsync(cts.Token);
                var response = Encoding.UTF8.GetString(result.Buffer).TrimEnd('\0').Trim();
#if ANDROID
                ReleaseMulticastLock();
#endif

                _logger.LogDebug("📥 Received response: {Response}", response);

                // Parse response: "MOBAFLOW_REST_API|192.168.0.100|5001"
                if (response.StartsWith(DiscoveryResponsePrefix, StringComparison.Ordinal))
                {
                    var parts = response.Split('|');
                    if (parts.Length >= 3 && int.TryParse(parts[2].Trim(), out var port) && port > 0 && port < 65536)
                    {
                        var ip = parts[1].Trim();
                        if (!string.IsNullOrEmpty(ip))
                        {
                            _logger.LogInformation("✅ Server discovered: {Ip}:{Port}", ip, port);
                            return (ip, port);
                        }
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
#if ANDROID
        finally
        {
            ReleaseMulticastLock();
        }
#endif

        _logger.LogInformation("ℹ️ Auto-discovery did not find server");
        return (null, null);
    }

#if ANDROID
    private static global::Android.Net.Wifi.WifiManager.MulticastLock? _multicastLock;

    private static void AcquireMulticastLock()
    {
        try
        {
            var ctx = global::Android.App.Application.Context;
            var wifi = (global::Android.Net.Wifi.WifiManager?)ctx?.GetSystemService(global::Android.Content.Context.WifiService);
            if (wifi != null)
            {
                _multicastLock = wifi.CreateMulticastLock("MOBAflow REST discovery");
                _multicastLock?.SetReferenceCounted(false);
                _multicastLock?.Acquire();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MulticastLock acquire failed: {ex.Message}");
        }
    }

    private static void ReleaseMulticastLock()
    {
        try
        {
            if (_multicastLock?.IsHeld == true)
                _multicastLock.Release();
            _multicastLock = null;
        }
        catch { /* ignore */ }
    }
#endif

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
