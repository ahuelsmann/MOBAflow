// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Android.App;
using Android.Content;
using Android.Net.Wifi;

using Common.Configuration;
using Common.Discovery;

using Microsoft.Extensions.Logging;

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

using Debug = System.Diagnostics.Debug;

/// <summary>
/// REST-API server discovery for MOBAsmart (smartphone on LAN).
/// Tries UDP multicast first, then scans the local /24 subnets for MOBApi HTTP health (same idea as Z21 subnet scan).
/// </summary>
public class RestApiDiscoveryService
{
    private const int DiscoveryPort = 21106;
    private const string DiscoveryRequest = "MOBAFLOW_DISCOVER";
    private const string MulticastAddress = "239.255.42.99";
    private const int DiscoveryTimeoutMs = 3000;
    private const int SubnetProbeBatchSize = 12;
    private const int SubnetProbeRequestTimeoutMs = 450;

    /// <summary>
    /// Dedicated LAN probe client: bypasses system proxy/VPN (avoids SOCKS for RFC1918) and avoids contention with the app-wide <see cref="HttpClient"/> singleton.
    /// </summary>
    private static readonly Lazy<HttpClient> LanProbeHttpClient = new(CreateLanProbeHttpClient);
    private const string MobApiHealthPath = "/api/photos/health";

    private readonly ILogger<RestApiDiscoveryService> _logger;
    private readonly AppSettings _appSettings;

    public RestApiDiscoveryService(
        ILogger<RestApiDiscoveryService> logger,
        AppSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    private static HttpClient CreateLanProbeHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromMilliseconds(600),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 128,
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(2),
        };
    }

    /// <summary>
    /// Returns the REST-API endpoint when found via multicast or LAN HTTP probe.
    /// </summary>
    public Task<(string? ip, int? port)> GetServerEndpointByDiscoveryOnlyAsync(CancellationToken cancellationToken = default) =>
        DiscoverServerAsync(cancellationToken);

    /// <summary>
    /// Attempts UDP multicast discovery, then scans private /24 subnets for MOBApi health.
    /// </summary>
    public async Task<(string? ip, int? port)> DiscoverServerAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Starting UDP Multicast discovery for MOBAflow server...");

        try
        {
#if ANDROID
            AcquireMulticastLock();
#endif
            using var udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            udpClient.Client.ReceiveTimeout = DiscoveryTimeoutMs;

            var requestBytes = Encoding.UTF8.GetBytes(DiscoveryRequest);
            var multicastEndpoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), DiscoveryPort);

            _logger.LogDebug("📤 Sending discovery request to {MulticastAddress}:{Port}",
                MulticastAddress, DiscoveryPort);

            await udpClient.SendAsync(requestBytes, requestBytes.Length, multicastEndpoint).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(DiscoveryTimeoutMs);

            try
            {
                var result = await udpClient.ReceiveAsync(cts.Token).ConfigureAwait(false);
                var response = Encoding.UTF8.GetString(result.Buffer).TrimEnd('\0').Trim();

                _logger.LogDebug("📥 Received response: {Response}", response);

                if (DiscoveryResponseParser.TryParse(response, out var ip, out var portVal) && ip != null && portVal != null)
                {
                    _logger.LogInformation("✅ Server discovered: {Ip}:{Port}", ip, portVal);
                    return (ip, portVal);
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
        catch (OperationCanceledException)
        {
            _logger.LogDebug("⏱️ Discovery timeout - no response received");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Multicast discovery failed");
        }
#if ANDROID
        finally
        {
            ReleaseMulticastLock();
        }
#endif

        var restPort = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;
        _logger.LogInformation(
            "ℹ️ Multicast did not find MOBApi — scanning LAN for HTTP health on port {Port} (fallback)",
            restPort);

        var probed = await TryDiscoverBySubnetHttpProbeAsync(restPort, cancellationToken).ConfigureAwait(false);
        if (probed.ip != null && probed.port.HasValue)
            return probed;

        _logger.LogInformation("ℹ️ Auto-discovery did not find server");
        return (null, null);
    }

    /// <summary>
    /// Probes hosts in the same /24 subnets as this device for MOBApi GET /api/photos/health.
    /// </summary>
    private async Task<(string? ip, int? port)> TryDiscoverBySubnetHttpProbeAsync(int restPort, CancellationToken cancellationToken)
    {
        var localAddresses = LanIpv4AddressHelper.GetCandidateLocalIpv4Addresses();
        var candidates = SubnetCandidateBuilder.BuildCandidates(localAddresses);
        if (candidates.Count == 0)
        {
            _logger.LogDebug("REST subnet probe: no subnet candidates (no suitable local IPv4)");
            return (null, null);
        }

        var orderedSubnet = OrderLanProbeCandidates(candidates, localAddresses);
        var ordered = MergePriorityRestEndpointCandidates(_appSettings, orderedSubnet);

        _logger.LogInformation(
            "REST subnet probe: up to {Count} hosts on port {Port} (saved/recent IPs first, then LAN order)",
            ordered.Count,
            restPort);

        foreach (var batch in ordered.Chunk(SubnetProbeBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tasks = batch.Select(ip => ProbeMobApiHealthAsync(ip.ToString(), restPort, cancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            var found = results.FirstOrDefault(r => !string.IsNullOrEmpty(r));
            if (found != null)
            {
                _logger.LogInformation("✅ MOBApi found by subnet probe: {Ip}:{Port}", found, restPort);
                return (found, restPort);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Probes same-/24 hosts with smaller last-octet distance to any local IPv4 first (typical DHCP clusters).
    /// </summary>
    private static List<IPAddress> OrderLanProbeCandidates(
        IReadOnlyList<IPAddress> candidates,
        IReadOnlyList<IPAddress> localAddresses)
    {
        var localBytes = localAddresses
            .Select(a => a.GetAddressBytes())
            .Where(b => b.Length == 4)
            .ToList();

        const int unmatchedSubnetScore = 512;

        int ProximityScore(IPAddress candidate)
        {
            var c = candidate.GetAddressBytes();
            if (c.Length != 4)
                return unmatchedSubnetScore;

            var best = unmatchedSubnetScore;
            foreach (var l in localBytes)
            {
                if (c[0] != l[0] || c[1] != l[1] || c[2] != l[2])
                    continue;

                var distance = Math.Abs(c[3] - l[3]);
                if (distance < best)
                    best = distance;
            }

            return best;
        }

        return candidates
            .OrderBy(ProximityScore)
            .ThenBy(ip =>
            {
                var b = ip.GetAddressBytes();
                return b.Length == 4 ? b[3] : 0;
            })
            .ToList();
    }

    /// <summary>
    /// Puts configured and recently used REST IPs at the front so a correct address from MOBAflow settings
    /// is reached before scanning the full /24 (reduces load and fixes stale defaults like an old DHCP lease).
    /// </summary>
    private static List<IPAddress> MergePriorityRestEndpointCandidates(
        AppSettings settings,
        IReadOnlyList<IPAddress> orderedSubnetCandidates)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<IPAddress>();

        void TryAddString(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return;
            if (!IPAddress.TryParse(s.AsSpan().Trim(), out var ip))
                return;
            TryAddAddress(ip);
        }

        void TryAddAddress(IPAddress ip)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork)
                return;
            if (!IsPrivateIpv4(ip))
                return;
            var key = ip.ToString();
            if (!seen.Add(key))
                return;
            result.Add(ip);
        }

        TryAddString(settings.RestApi.CurrentIpAddress);
        foreach (var r in settings.RestApi.RecentIpAddresses)
            TryAddString(r);

        foreach (var c in orderedSubnetCandidates)
            TryAddAddress(c);

        return result;
    }

    private static bool IsPrivateIpv4(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        if (b.Length != 4)
            return false;

        // 10.0.0.0/8
        if (b[0] == 10)
            return true;

        // 172.16.0.0/12
        if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            return true;

        // 192.168.0.0/16
        return b[0] == 192 && b[1] == 168;
    }

    private async Task<string?> ProbeMobApiHealthAsync(string ip, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(SubnetProbeRequestTimeoutMs);
            var url = $"http://{ip}:{port}{MobApiHealthPath}";
            using var response = await LanProbeHttpClient.Value
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode ? ip : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "REST probe miss at {Ip}:{Port}", ip, port);
            return null;
        }
    }

#if ANDROID
    private static WifiManager.MulticastLock? _multicastLock;

    private static void AcquireMulticastLock()
    {
        try
        {
            var ctx = Application.Context;
            var wifi = (WifiManager?)ctx.GetSystemService(Context.WifiService);
            if (wifi != null)
            {
                _multicastLock = wifi.CreateMulticastLock("MOBAflow REST discovery");
                _multicastLock?.SetReferenceCounted(false);
                _multicastLock?.Acquire();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MulticastLock acquire failed: {ex.Message}");
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
}
