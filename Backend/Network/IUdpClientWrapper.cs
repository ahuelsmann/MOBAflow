// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Network;

using System.Net;

public interface IUdpClientWrapper : IDisposable
{
    event EventHandler<UdpReceivedEventArgs>? Received;

    /// <summary>
    /// Indicates whether the UDP wrapper is connected and ready to send/receive.
    /// </summary>
    bool IsConnected { get; }

    Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default);
    Task SendAsync(byte[] data, CancellationToken cancellationToken = default, int maxRetries = 3);
    Task StopAsync();
}
