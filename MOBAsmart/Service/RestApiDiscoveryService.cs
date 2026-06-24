// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Android.App;
using Android.Content;
using Android.Net.Wifi;

using Common.Configuration;
using Common.Discovery;

using SharedUI.Interface;

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Discovers the MOBAflow REST API (MOBApi) on the LAN for MOBAsmart.
/// Order: recent/nearby HTTP probe, UDP multicast/broadcast, anchor subnet HTTP, then full /24 HTTP scan.
/// </summary>
public class RestApiDiscoveryService : IRestDiscoveryService
{
    private const int MulticastReceiveTimeoutMs = 1200;
    private const int SubnetProbeBatchSize = 16;
    private const int QuickProbeTimeoutMs = 350;
#if ANDROID
    private const int SubnetProbeRequestTimeoutMs = 1200;
#else
    private const int SubnetProbeRequestTimeoutMs = 600;
#endif

    private readonly AppSettings _appSettings;
    private readonly HttpClient _lanProbeHttpClient;

    public RestApiDiscoveryService(AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _appSettings = appSettings;
        _lanProbeHttpClient = httpClientFactory.CreateClient(MobiHttpClientNames.LanDiscovery);
    }

    /// <summary>
    /// Returns the REST-API endpoint when found via LAN discovery.
    /// </summary>
    public Task<(string? ip, int? port)> GetServerEndpointByDiscoveryOnlyAsync(
        string? subnetAnchorIp = null,
        CancellationToken cancellationToken = default) =>
        DiscoverServerAsync(subnetAnchorIp, cancellationToken);

    /// <summary>
    /// Fast discovery path: recent IPs and UDP only (no full subnet scan).
    /// </summary>
    public async Task<(string? ip, int? port)> DiscoverServerFastAsync(
        string? subnetAnchorIp = null,
        CancellationToken cancellationToken = default)
    {
        var restPort = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;
        var localAddresses = LanIpv4AddressHelper.GetCandidateLocalIpv4Addresses();

        var quickTask = TryDiscoverByQuickHttpProbeAsync(localAddresses, restPort, cancellationToken);
        var udpTask = TryDiscoverByUdpAsync(cancellationToken);

        var firstFinished = await Task.WhenAny(quickTask, udpTask).ConfigureAwait(false);
        var firstResult = await firstFinished.ConfigureAwait(false);
        if (firstResult.ip != null && firstResult.port.HasValue)
        {
            return firstResult;
        }

        var secondResult = firstFinished == quickTask
            ? await udpTask.ConfigureAwait(false)
            : await quickTask.ConfigureAwait(false);
        if (secondResult.ip != null && secondResult.port.HasValue)
        {
            return secondResult;
        }

        if (!string.IsNullOrWhiteSpace(subnetAnchorIp)
            && IPAddress.TryParse(subnetAnchorIp.Trim(), out var anchor))
        {
            try
            {
                return await TryDiscoverByAnchorSubnetAsync(anchor, localAddresses, restPort, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Attempts recent HTTP probe, UDP discovery, anchor subnet scan, then full /24 HTTP scan.
    /// </summary>
    /// <param name="subnetAnchorIp">Optional Z21 or other LAN hint; its /24 is scanned after UDP.</param>
    public async Task<(string? ip, int? port)> DiscoverServerAsync(
        string? subnetAnchorIp = null,
        CancellationToken cancellationToken = default)
    {
        var restPort = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;
        var localAddresses = LanIpv4AddressHelper.GetCandidateLocalIpv4Addresses();

        try
        {
            var quick = await TryDiscoverByQuickHttpProbeAsync(localAddresses, restPort, cancellationToken)
                .ConfigureAwait(false);
            if (quick.ip != null && quick.port.HasValue)
            {
                return quick;
            }
        }
        catch (Exception)
        {
        }

        try
        {
            var udp = await TryDiscoverByUdpAsync(cancellationToken).ConfigureAwait(false);
            if (udp.ip != null && udp.port.HasValue)
            {
                return udp;
            }
        }
        catch (Exception)
        {
        }

        if (!string.IsNullOrWhiteSpace(subnetAnchorIp)
            && IPAddress.TryParse(subnetAnchorIp.Trim(), out var anchor))
        {
            try
            {
                var anchored = await TryDiscoverByAnchorSubnetAsync(anchor, localAddresses, restPort, cancellationToken)
                    .ConfigureAwait(false);
                if (anchored.ip != null && anchored.port.HasValue)
                {
                    return anchored;
                }
            }
            catch (Exception)
            {
            }
        }

        try
        {
            var probed = await TryDiscoverBySubnetHttpProbeAsync(localAddresses, restPort, cancellationToken)
                .ConfigureAwait(false);
            if (probed.ip != null && probed.port.HasValue)
            {
                return probed;
            }
        }
        catch (Exception)
        {
        }

        return (null, null);
    }

    private async Task<(string? ip, int? port)> TryDiscoverByAnchorSubnetAsync(
        IPAddress anchor,
        IReadOnlyList<IPAddress> localAddresses,
        int restPort,
        CancellationToken cancellationToken)
    {
        var candidates = RestApiDiscoveryCandidateBuilder.BuildSubnetFromAnchor(anchor);
        if (candidates.Count == 0)
        {
            return (null, null);
        }

        // Order by the phone's LAN IP (not Z21): MOBAflow is often nearer the handset than the Z21.
        var proximitySources = localAddresses.Count > 0 ? localAddresses : (IReadOnlyList<IPAddress>)[anchor];
        var ordered = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(
            _appSettings.RestApi,
            proximitySources,
            candidates);

        foreach (var batch in ordered.Chunk(SubnetProbeBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var found = await ProbeFirstHealthyHostAsync(batch, restPort, SubnetProbeRequestTimeoutMs, cancellationToken)
                .ConfigureAwait(false);
            if (found != null)
            {
                return (found, restPort);
            }
        }

        return (null, null);
    }

    private async Task<(string? ip, int? port)> TryDiscoverByQuickHttpProbeAsync(
        IReadOnlyList<IPAddress> localAddresses,
        int restPort,
        CancellationToken cancellationToken)
    {
        if (localAddresses.Count == 0)
        {
            return (null, null);
        }

        var candidates = new List<IPAddress>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void AddIp(IPAddress ip)
        {
            var key = ip.ToString();
            if (seen.Add(key))
            {
                candidates.Add(ip);
            }
        }

        foreach (var recent in _appSettings.RestApi.RecentIpAddresses ?? [])
        {
            if (string.IsNullOrWhiteSpace(recent) || !IPAddress.TryParse(recent.Trim(), out var recentIp))
            {
                continue;
            }

            AddIp(recentIp);
        }

        foreach (var nearby in RestApiDiscoveryCandidateBuilder.BuildQuickWindowCandidates(localAddresses))
        {
            AddIp(nearby);
        }

        if (candidates.Count == 0)
        {
            return (null, null);
        }

        var found = await ProbeFirstHealthyHostAsync(candidates, restPort, QuickProbeTimeoutMs, cancellationToken)
            .ConfigureAwait(false);
        return found != null ? (found, restPort) : (null, null);
    }

    private async Task<(string? ip, int? port)> TryDiscoverByUdpAsync(CancellationToken cancellationToken)
    {
#if ANDROID
        AcquireMulticastLock();
#endif
        try
        {
            using var udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            var multicastAddress = IPAddress.Parse(DiscoveryResponseParser.MulticastAddress);
            try
            {
                udpClient.JoinMulticastGroup(multicastAddress);
            }
            catch (SocketException)
            {
                // Some platforms reject join; unicast replies may still work.
            }

            var requestBytes = Encoding.UTF8.GetBytes(DiscoveryResponseParser.RequestMessage);
            var multicastEndpoint = new IPEndPoint(multicastAddress, DiscoveryResponseParser.MulticastPort);
            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryResponseParser.MulticastPort);

            await udpClient.SendAsync(requestBytes, requestBytes.Length, multicastEndpoint).ConfigureAwait(false);
            await udpClient.SendAsync(requestBytes, requestBytes.Length, broadcastEndpoint).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(MulticastReceiveTimeoutMs);

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await udpClient.ReceiveAsync(cts.Token).ConfigureAwait(false);
                    var response = Encoding.UTF8.GetString(result.Buffer).TrimEnd('\0').Trim();

                    if (DiscoveryResponseParser.TryParse(response, out var ip, out var portVal)
                        && ip != null
                        && portVal.HasValue
                        && await ProbeMobApiHealthAsync(ip, portVal.Value, SubnetProbeRequestTimeoutMs, cts.Token)
                            .ConfigureAwait(false) != null)
                    {
                        return (ip, portVal);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException socketException) when (socketException.SocketErrorCode == SocketError.TimedOut)
            {
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
#if ANDROID
        finally
        {
            ReleaseMulticastLock();
        }
#endif

        return (null, null);
    }

    private async Task<(string? ip, int? port)> TryDiscoverBySubnetHttpProbeAsync(
        IReadOnlyList<IPAddress> localAddresses,
        int restPort,
        CancellationToken cancellationToken)
    {
        var subnetCandidates = SubnetCandidateBuilder.BuildCandidates(localAddresses);
        if (subnetCandidates.Count == 0)
        {
            return (null, null);
        }

        var ordered = RestApiDiscoveryCandidateBuilder.BuildFullProbeOrder(
            _appSettings.RestApi,
            localAddresses,
            subnetCandidates);

        foreach (var batch in ordered.Chunk(SubnetProbeBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var found = await ProbeFirstHealthyHostAsync(batch, restPort, SubnetProbeRequestTimeoutMs, cancellationToken)
                .ConfigureAwait(false);
            if (found != null)
            {
                return (found, restPort);
            }
        }

        return (null, null);
    }

    private async Task<string?> ProbeFirstHealthyHostAsync(
        IEnumerable<IPAddress> candidates,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        var tasks = candidates
            .Select(ip => ProbeMobApiHealthAsync(ip.ToString(), port, timeoutMs, cancellationToken))
            .ToArray();
        if (tasks.Length == 0)
        {
            return null;
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.FirstOrDefault(r => !string.IsNullOrEmpty(r));
    }

    private async Task<string?> ProbeMobApiHealthAsync(
        string ip,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);
            var url = $"http://{ip}:{port}{MobApiHealthProbe.HealthPath}";
            using var response = await _lanProbeHttpClient
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            return MobApiHealthProbe.IsHealthyResponse(body) ? ip : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

#if ANDROID
    private static WifiManager.MulticastLock? _multicastLock;

    private void AcquireMulticastLock()
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
        catch (Exception)
        {
        }
    }

    private static void ReleaseMulticastLock()
    {
        try
        {
            if (_multicastLock?.IsHeld == true)
            {
                _multicastLock.Release();
            }

            _multicastLock = null;
        }
        catch
        {
            // Ignore release failures.
        }
    }
#endif
}
