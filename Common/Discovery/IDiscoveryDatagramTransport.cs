// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Discovery;

using System.Net;
using System.Net.Sockets;

internal interface IDiscoveryDatagramTransport : IDisposable
{
    void BindAndJoinMulticast(IPEndPoint localEndPoint, IPAddress multicastAddress);
    ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken);
    ValueTask<int> SendAsync(ReadOnlyMemory<byte> payload, IPEndPoint remoteEndPoint, CancellationToken cancellationToken);
    void Close();
}

internal sealed class UdpDiscoveryDatagramTransport : IDiscoveryDatagramTransport
{
    private readonly UdpClient _client = new() { ExclusiveAddressUse = false };

    public void BindAndJoinMulticast(IPEndPoint localEndPoint, IPAddress multicastAddress)
    {
        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _client.Client.Bind(localEndPoint);
        _client.JoinMulticastGroup(multicastAddress);
    }

    public ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken) => _client.ReceiveAsync(cancellationToken);

    public ValueTask<int> SendAsync(ReadOnlyMemory<byte> payload, IPEndPoint remoteEndPoint, CancellationToken cancellationToken) =>
        _client.SendAsync(payload, remoteEndPoint, cancellationToken);

    public void Close() => _client.Close();

    public void Dispose() => _client.Dispose();
}
