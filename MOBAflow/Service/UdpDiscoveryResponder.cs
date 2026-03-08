// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP Discovery Responder for MOBAflow REST-API.
/// Listens for UDP Multicast discovery requests from MAUI clients and responds with server IP + Port.
/// Runs alongside Kestrel to enable automatic server discovery on local network.
/// </summary>
internal sealed partial class UdpDiscoveryResponder : IDisposable
{
    private const int DiscoveryPort = 21106;
    private const string DiscoveryRequest = "MOBAFLOW_DISCOVER";
    private const string DiscoveryResponsePrefix = "MOBAFLOW_REST_API";
    private const string MulticastAddress = "239.255.42.99";

    private readonly ILogger<UdpDiscoveryResponder> _logger;
    private readonly int _restApiPort;
    private UdpClient? _udpListener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private bool _disposed;

    public UdpDiscoveryResponder(ILogger<UdpDiscoveryResponder> logger, int restApiPort)
    {
        _logger = logger;
        _restApiPort = restApiPort;
    }

    /// <summary>
    /// Starts the UDP Discovery responder.
    /// </summary>
    public void Start()
    {
        if (_listenerTask != null)
        {
            _logger.LogWarning("UDP Discovery responder already running");
            return;
        }

        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(() => ListenAsync(_cts.Token));
        _logger.LogInformation("🔍 UDP Discovery responder started on Multicast {MulticastAddress}:{Port}",
            MulticastAddress, DiscoveryPort);
    }

    /// <summary>
    /// Stops the UDP Discovery responder.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _udpListener?.Close();
        _logger.LogInformation("UDP Discovery responder stopped");
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            _udpListener = new UdpClient
            {
                ExclusiveAddressUse = false
            };

            var localEndPoint = new IPEndPoint(IPAddress.Any, DiscoveryPort);
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.Bind(localEndPoint);

            // Join multicast on all interfaces so we receive discovery from any NIC (Wi‑Fi, Ethernet, etc.)
            var multicastAddress = IPAddress.Parse(MulticastAddress);
            _udpListener.JoinMulticastGroup(multicastAddress);

            _logger.LogInformation("✅ Joined Multicast group {MulticastAddress}:{Port} - listening for MAUI discovery",
                MulticastAddress, DiscoveryPort);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync(cancellationToken);
                    var message = Encoding.UTF8.GetString(result.Buffer);

                    if (message.Trim() == DiscoveryRequest)
                    {
                        _logger.LogInformation("📱 Discovery request received from {RemoteEndPoint}", result.RemoteEndPoint);

                        // Advertise the local IP that is reachable from the client (same subnet)
                        var advertisedIp = GetLocalIpAddressForRemote(result.RemoteEndPoint);
                        var response = $"{DiscoveryResponsePrefix}|{advertisedIp}|{_restApiPort}";
                        var responseBytes = Encoding.UTF8.GetBytes(response);

                        await _udpListener.SendAsync(responseBytes, result.RemoteEndPoint, cancellationToken);

                        _logger.LogInformation("📤 Discovery response sent: {Response}", response);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UDP discovery request");
                }
            }
        }
        catch (SocketException ex)
        {
            _logger.LogWarning("UDP Discovery responder could not bind to port {Port}: {Message}",
                DiscoveryPort, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP Discovery responder failed");
        }
        finally
        {
            _udpListener?.Close();
        }
    }

    /// <summary>
    /// Gets the local IP address reachable from the remote endpoint (prefer same subnet so client can connect).
    /// </summary>
    private static string GetLocalIpAddressForRemote(IPEndPoint? remote)
    {
        if (remote?.Address == null)
            return GetLocalIpAddress();
        return GetLocalIpAddressInSubnet(remote.Address) ?? GetLocalIpAddress();
    }

    /// <summary>
    /// Returns a local IPv4 address in the same subnet as the given remote address, or null.
    /// </summary>
    private static string? GetLocalIpAddressInSubnet(IPAddress remoteIp)
    {
        if (remoteIp.AddressFamily != AddressFamily.InterNetwork)
            return null;
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var remoteBytes = remoteIp.GetAddressBytes();
            foreach (var local in host.AddressList)
            {
                if (local.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (IsInSameSubnet(local.GetAddressBytes(), remoteBytes, 24))
                    return local.ToString();
                if (IsInSameSubnet(local.GetAddressBytes(), remoteBytes, 16))
                    return local.ToString();
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private static bool IsInSameSubnet(byte[] local, byte[] remote, int prefixLength)
    {
        if (local.Length != 4 || remote.Length != 4) return false;
        int fullBytes = prefixLength / 8;
        int remainder = prefixLength % 8;
        for (int i = 0; i < fullBytes; i++)
            if (local[i] != remote[i]) return false;
        if (remainder > 0)
        {
            int mask = (0xFF << (8 - remainder)) & 0xFF;
            if ((local[fullBytes] & mask) != (remote[fullBytes] & mask)) return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the local IP address of the server (prefers 192.168.x.x for home networks).
    /// </summary>
    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            // Prefer 192.168.x.x addresses (common home network range)
            var privateIp = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                   && ip.ToString().StartsWith("192.168."));

            if (privateIp != null)
                return privateIp.ToString();

            // Fallback: 10.x.x.x range
            var tenRange = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                   && ip.ToString().StartsWith("10."));

            if (tenRange != null)
                return tenRange.ToString();

            // Fallback: Any IPv4 address
            var anyIp = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            return anyIp?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _cts?.Dispose();
        _udpListener?.Dispose();
    }
}
