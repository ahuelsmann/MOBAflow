// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Discovery;

using Common.Discovery;
using Protocol;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Discovers a Z21 command station on the local network by scanning the subnet for UDP ports 21105/21106
/// and verifying responses with the Z21 handshake (LAN_SYSTEMSTATE_GETDATA).
/// </summary>
public sealed class Z21DiscoveryService : IZ21DiscoveryService
{
    private static readonly int[] DiscoveryPorts = [Z21Protocol.DefaultPort, Z21Protocol.AlternativePort];

    private const int SendReceiveTimeoutMs = 400;
    /// <summary>After sending to all candidates, wait this long for the first response.</summary>
    private const int ReceiveAnyTimeoutMs = 2000;
    private const int PreferredProbeTimeoutMs = 800;

    /// <inheritdoc />
    public async Task<string?> DiscoverZ21Async(string? preferredIpAddress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(preferredIpAddress)
                && IPAddress.TryParse(preferredIpAddress.Trim(), out var preferred))
            {
                var preferredResult = await ProbeEndpointAsync(preferred, PreferredProbeTimeoutMs, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrEmpty(preferredResult))
                {
                    return preferredResult;
                }
            }

            foreach (var port in DiscoveryPorts)
            {
                var scanResult = await ScanSubnetsAsync(port, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(scanResult))
                {
                    return scanResult;
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<string?> ScanSubnetsAsync(int port, CancellationToken cancellationToken)
    {
        var localAddresses = LanIpv4AddressHelper.GetCandidateLocalIpv4Addresses();
        var candidates = SubnetCandidateBuilder.BuildCandidates(localAddresses);
        if (candidates.Count == 0)
        {
            return null;
        }

        var handshake = Z21Command.BuildHandshake();

        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = ReceiveAnyTimeoutMs;
        udp.Client.SendTimeout = SendReceiveTimeoutMs;

        foreach (var ip in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var endpoint = new IPEndPoint(ip, port);
            try
            {
                udp.Send(handshake, handshake.Length, endpoint);
            }
            catch (SocketException)
            {
                // Skip unreachable; continue with others
            }
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ReceiveAnyTimeoutMs);
        try
        {
            var result = await udp.ReceiveAsync(cts.Token).ConfigureAwait(false);
            return IsZ21Response(result.Buffer) ? result.RemoteEndPoint.Address.ToString() : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private static async Task<string?> ProbeEndpointAsync(
        IPAddress address,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        var handshake = Z21Command.BuildHandshake();

        foreach (var port in DiscoveryPorts)
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = timeoutMs;
            udp.Client.SendTimeout = SendReceiveTimeoutMs;

            var endpoint = new IPEndPoint(address, port);
            try
            {
                udp.Send(handshake, handshake.Length, endpoint);
            }
            catch (SocketException)
            {
                continue;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);
            try
            {
                var result = await udp.ReceiveAsync(cts.Token).ConfigureAwait(false);
                if (IsZ21Response(result.Buffer))
                {
                    return result.RemoteEndPoint.Address.ToString();
                }
            }
            catch (OperationCanceledException)
            {
                // Try next port
            }
        }

        return null;
    }

    /// <summary>
    /// Returns true if the packet looks like a Z21 response (e.g. LAN_SYSTEMSTATE_DATACHANGED or any LAN_X / LAN_ header).
    /// </summary>
    public static bool IsZ21Response(byte[] data)
    {
        if (data.Length < 4)
        {
            return false;
        }

        ushort dataLen = (ushort)(data[0] | (data[1] << 8));
        if (dataLen < 4 || dataLen > 1024)
        {
            return false;
        }

        byte h2 = data[2];
        byte h3 = data[3];
        if (h3 != 0x00)
        {
            return false;
        }

        return h2 == Z21Protocol.Header.LAN_SYSTEMSTATE
               || h2 == Z21Protocol.Header.LAN_X_HEADER
               || h2 == Z21Protocol.Header.LAN_GET_SERIAL_NUMBER
               || h2 == Z21Protocol.Header.LAN_GET_HWINFO;
    }
}
