// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Net;
using Moba.Backend.Network;

namespace Moba.Test.Mocks;

public sealed class FakeUdpClientWrapper : IUdpClientWrapper
{
    public event EventHandler<UdpReceivedEventArgs>? Received;

    public bool Connected { get; private set; }
    public List<byte[]> SentPayloads { get; } = new();

    public Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default)
    {
        Connected = true;
        return Task.CompletedTask;
    }

    public Task SendAsync(byte[] data, CancellationToken cancellationToken = default, int maxRetries = 3)
    {
        SentPayloads.Add(data);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Connected = false;
        return Task.CompletedTask;
    }

    public void RaiseReceived(byte[] payload, IPEndPoint? remote = null)
    {
        Received?.Invoke(this, new UdpReceivedEventArgs(payload, remote ?? new IPEndPoint(IPAddress.Loopback, 21105)));
    }

    public void Dispose() { }
}
