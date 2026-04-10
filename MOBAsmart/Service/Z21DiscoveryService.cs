// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Backend.Protocol;

using Common.Discovery;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Net;
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
    private const int ReceiveAnyTimeoutMs = 1000;

    private readonly ILogger<Z21DiscoveryService> _logger;

    public Z21DiscoveryService(ILogger<Z21DiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to discover a Z21 on the local network by scanning every reachable /24 subnet of the active IPv4 interfaces.
    /// Sends a Z21 handshake to all candidate IPs in quick succession, then waits for the first response.
    /// This is much faster than probing each IP sequentially (typically under 1 second if Z21 is present).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>IP address of the first responding Z21, or null if none found.</returns>
    public async Task<string?> DiscoverZ21Async(CancellationToken cancellationToken = default)
    {
        var localAddresses = LanIpv4AddressHelper.GetCandidateLocalIpv4Addresses();
        var candidates = SubnetCandidateBuilder.BuildCandidates(localAddresses);
        if (candidates.Count == 0)
        {
            _logger.LogWarning("Z21 discovery: no subnet candidates (no suitable local IPv4)");
            return null;
        }

        _logger.LogInformation(
            "Z21 discovery: sending handshake to {Count} addresses derived from {LocalAddressCount} local IPv4 addresses on port {Port}",
            candidates.Count,
            localAddresses.Count,
            Z21Port);
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
                _logger.LogInformation("Z21 discovered at {Ip}", result.RemoteEndPoint.Address);
                return result.RemoteEndPoint.Address.ToString();
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
    /// Returns true if the packet looks like a Z21 response (e.g. LAN_SYSTEMSTATE_DATACHANGED or any LAN_X / LAN_ header).
    /// </summary>
    private static bool IsZ21Response(byte[] data)
    {
        if (data.Length < 4)
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
               h2 == Z21Protocol.Header.LAN_GET_HWINFO;
    }
}
