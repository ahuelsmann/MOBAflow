// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP discovery responder for the MOBAflow REST API (MOBApi).
/// Listens for <see cref="DiscoveryResponseParser.RequestMessage"/> on the MOBAflow multicast port
/// and replies with <see cref="DiscoveryResponseParser.ResponsePrefix"/>|ip|port.
/// </summary>
public sealed class MobApiUdpDiscoveryResponder : IDisposable
{
    private readonly int _restApiPort;
    private readonly int? _httpsPort;
    private readonly string? _serverInstanceId;
    private readonly string? _serverPublicKeyFingerprint;
    private UdpClient? _udpListener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private bool _disposed;

    /// <summary>
    /// Creates a responder that advertises the given REST API port.
    /// </summary>
    public MobApiUdpDiscoveryResponder(int restApiPort)
    {
        if (restApiPort <= 0 || restApiPort >= 65536)
        {
            throw new ArgumentOutOfRangeException(nameof(restApiPort));
        }

        _restApiPort = restApiPort;
    }

    /// <summary>
    /// Creates a responder that advertises the legacy HTTP endpoint plus authenticated HTTPS metadata.
    /// </summary>
    public MobApiUdpDiscoveryResponder(
        int restApiPort,
        int httpsPort,
        string serverInstanceId,
        string serverPublicKeyFingerprint)
        : this(restApiPort)
    {
        if (httpsPort <= 0 || httpsPort >= 65536)
            throw new ArgumentOutOfRangeException(nameof(httpsPort));

        _httpsPort = httpsPort;
        _serverInstanceId = serverInstanceId;
        _serverPublicKeyFingerprint = serverPublicKeyFingerprint;

        _ = DiscoveryResponseParser.CreateResponse(
            "127.0.0.1",
            restApiPort,
            httpsPort,
            serverInstanceId,
            serverPublicKeyFingerprint);
    }

    /// <summary>
    /// Starts listening for discovery requests on a background thread.
    /// </summary>
    public void Start()
    {
        if (_listenerTask != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(() => ListenAsync(_cts.Token));
    }

    /// <summary>
    /// Stops the discovery responder.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _udpListener?.Close();
        _listenerTask = null;
    }

    /// <summary>
    /// Runs the discovery listener until cancelled. Intended for hosted services.
    /// </summary>
    public Task RunAsync(CancellationToken cancellationToken) => ListenAsync(cancellationToken);

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            _udpListener = new UdpClient { ExclusiveAddressUse = false };

            var localEndPoint = new IPEndPoint(IPAddress.Any, DiscoveryResponseParser.MulticastPort);
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.Bind(localEndPoint);

            var multicastAddress = IPAddress.Parse(DiscoveryResponseParser.MulticastAddress);
            _udpListener.JoinMulticastGroup(multicastAddress);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                    var message = Encoding.UTF8.GetString(result.Buffer);

                    if (message.Trim() != DiscoveryResponseParser.RequestMessage)
                    {
                        continue;
                    }

                    var advertisedIp = MobApiDiscoveryAddressResolver.GetLocalIpAddressForRemote(result.RemoteEndPoint);
                    var response = CreateResponse(advertisedIp);
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await _udpListener.SendAsync(responseBytes, result.RemoteEndPoint, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    break;
                }
            }
        }
        finally
        {
            _udpListener?.Close();
        }
    }

    private string CreateResponse(string advertisedIp) =>
        _httpsPort.HasValue &&
        _serverInstanceId is not null &&
        _serverPublicKeyFingerprint is not null
            ? DiscoveryResponseParser.CreateResponse(
                advertisedIp,
                _restApiPort,
                _httpsPort.Value,
                _serverInstanceId,
                _serverPublicKeyFingerprint)
            : $"{DiscoveryResponseParser.ResponsePrefix}|{advertisedIp}|{_restApiPort}";

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _cts?.Dispose();
        _udpListener?.Dispose();
    }
}

/// <summary>
/// Resolves the local IPv4 address to advertise to a remote discovery client.
/// </summary>
public static class MobApiDiscoveryAddressResolver
{
    /// <summary>
    /// Gets the local IP address reachable from the remote endpoint (prefer same subnet so client can connect).
    /// </summary>
    public static string GetLocalIpAddressForRemote(IPEndPoint? remote)
    {
        if (remote?.Address == null)
        {
            return GetLocalIpAddress();
        }

        return GetLocalIpAddressInSubnet(remote.Address) ?? GetLocalIpAddress();
    }

    /// <summary>
    /// Returns a local IPv4 address in the same subnet as the given remote address, or null.
    /// </summary>
    public static string? GetLocalIpAddressInSubnet(IPAddress remoteIp)
    {
        if (remoteIp.AddressFamily != AddressFamily.InterNetwork)
        {
            return null;
        }

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var remoteBytes = remoteIp.GetAddressBytes();
            foreach (var local in host.AddressList)
            {
                if (local.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                if (IsInSameSubnet(local.GetAddressBytes(), remoteBytes, 24)
                    || IsInSameSubnet(local.GetAddressBytes(), remoteBytes, 16))
                {
                    return local.ToString();
                }
            }
        }
        catch
        {
            // Ignore DNS failures
        }

        return null;
    }

    /// <summary>
    /// Gets a best-effort local IPv4 address for LAN advertisement.
    /// </summary>
    public static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var candidates = host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToList();

            var preferred = candidates.FirstOrDefault(ip => ip.StartsWith("192.168.", StringComparison.Ordinal))
                            ?? candidates.FirstOrDefault(ip => ip.StartsWith("10.", StringComparison.Ordinal))
                            ?? candidates.FirstOrDefault(ip => ip.StartsWith("172.", StringComparison.Ordinal));

            return preferred ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    private static bool IsInSameSubnet(byte[] local, byte[] remote, int prefixLength)
    {
        if (local.Length != 4 || remote.Length != 4)
        {
            return false;
        }

        int fullBytes = prefixLength / 8;
        int remainder = prefixLength % 8;
        for (int i = 0; i < fullBytes; i++)
        {
            if (local[i] != remote[i])
            {
                return false;
            }
        }

        if (remainder <= 0)
        {
            return true;
        }

        int mask = (0xFF << (8 - remainder)) & 0xFF;
        return (local[fullBytes] & mask) == (remote[fullBytes] & mask);
    }
}