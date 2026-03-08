// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Backend.Protocol;
using Microsoft.Extensions.Logging;
using SharedUI.Interface;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// Discovers a Z21 command station on the local network by scanning the subnet for UDP port 21105
/// and verifying responses with the Z21 handshake (LAN_SYSTEMSTATE_GETDATA).
/// </summary>
public class Z21DiscoveryService : IZ21DiscoveryService
{
    private const int Z21Port = 21105;
    private const int SendReceiveTimeoutMs = 400;
    /// <summary>After sending to all candidates, wait this long for the first response.</summary>
    private const int ReceiveAnyTimeoutMs = 600;

    private readonly ILogger<Z21DiscoveryService> _logger;

    public Z21DiscoveryService(ILogger<Z21DiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to discover a Z21 on the local network by scanning the same subnet as this device.
    /// Sends a Z21 handshake to all candidate IPs in quick succession, then waits for the first response.
    /// This is much faster than probing each IP sequentially (typically under 1 second if Z21 is present).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>IP address of the first responding Z21, or null if none found.</returns>
    public async Task<string?> DiscoverZ21Async(CancellationToken cancellationToken = default)
    {
        var candidates = GetSubnetCandidates();
        if (candidates.Count == 0)
        {
            _logger.LogWarning("Z21 discovery: no subnet candidates (no suitable local IPv4)");
            return null;
        }

        _logger.LogInformation("Z21 discovery: sending handshake to {Count} addresses on port {Port}", candidates.Count, Z21Port);
        var handshake = Z21Command.BuildHandshake();

        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = ReceiveAnyTimeoutMs;
        udp.Client.SendTimeout = SendReceiveTimeoutMs;

        // Send handshake to all candidates as fast as possible (no per-IP wait)
        foreach (var ip in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;
            var endpoint = new IPEndPoint(ip, Z21Port);
            try
            {
                udp.Send(handshake, handshake.Length, endpoint);
            }
            catch (SocketException)
            {
                // Skip unreachable; continue with others
            }
        }

        // Wait for the first Z21 response from any of them
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ReceiveAnyTimeoutMs);
        try
        {
            var result = await udp.ReceiveAsync(cts.Token).ConfigureAwait(false);
            var data = result.Buffer;
            if (IsZ21Response(data))
            {
                _logger.LogInformation("Z21 discovered at {Ip}", result.RemoteEndPoint?.Address);
                return result.RemoteEndPoint?.Address?.ToString();
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout or user cancel
        }

        _logger.LogInformation("Z21 discovery: no Z21 found on subnet");
        return null;
    }

    /// <summary>
    /// Gets a list of IPv4 addresses to scan (same subnet as the first private local address).
    /// Skips .0, .255 and the local address.
    /// </summary>
    private static List<IPAddress> GetSubnetCandidates()
    {
        var local = GetFirstPrivateIPv4();
        if (local == null)
            return [];

        var bytes = local.GetAddressBytes();
        if (bytes.Length != 4)
            return [];

        var list = new List<IPAddress>(254);
        for (int last = 1; last <= 254; last++)
        {
            if (last == bytes[3])
                continue;
            list.Add(new IPAddress(new byte[] { bytes[0], bytes[1], bytes[2], (byte)last }));
        }
        return list;
    }

    private static IPAddress? GetFirstPrivateIPv4()
    {
        try
        {
            IPAddress? fallbackAny = null;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    var ip = ua.Address.ToString();
                    if (ip.StartsWith("192.168.") || ip.StartsWith("10."))
                        return ua.Address;
#if ANDROID
                    // On Android, interfaces may not be reported as 192.168/10; keep first non-loopback IPv4 as fallback.
                    if (fallbackAny == null && !IPAddress.IsLoopback(ua.Address))
                        fallbackAny = ua.Address;
#endif
                }
            }
#if ANDROID
            return fallbackAny;
#else
            return null;
#endif
        }
        catch
        {
            // Ignore: e.g. permission or platform
        }

        return null;
    }

    /// <summary>
    /// Returns true if the packet looks like a Z21 response (e.g. LAN_SYSTEMSTATE_DATACHANGED or any LAN_X / LAN_ header).
    /// </summary>
    private static bool IsZ21Response(byte[] data)
    {
        if (data == null || data.Length < 4)
            return false;
        ushort dataLen = (ushort)(data[0] | (data[1] << 8));
        if (dataLen < 4 || dataLen > 1024)
            return false;
        byte h2 = data[2];
        byte h3 = data[3];
        if (h3 != 0x00)
            return false;
        return h2 == Z21Protocol.Header.LAN_SYSTEMSTATE ||
               h2 == Z21Protocol.Header.LAN_X_HEADER ||
               h2 == Z21Protocol.Header.LAN_GET_SERIAL_NUMBER ||
               h2 == Z21Protocol.Header.LAN_GET_HWINFO ||
               h2 == 0x10;
    }
}
