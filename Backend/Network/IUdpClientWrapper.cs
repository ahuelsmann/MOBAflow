// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Net;

namespace Moba.Backend.Network;

public interface IUdpClientWrapper : IDisposable
{
    event EventHandler<UdpReceivedEventArgs>? Received;
    Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default);
    Task SendAsync(byte[] data, CancellationToken cancellationToken = default, int maxRetries = 3);
    Task StopAsync();
}
