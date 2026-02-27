// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Network;

using System.Net;

/// <summary>
/// Abstraction over a UDP client used for Z21 communication.
/// Provides async connect, send and stop operations plus a receive event.
/// </summary>
public interface IUdpClientWrapper : IDisposable
{
    /// <summary>
    /// Raised whenever a UDP datagram has been received from the remote endpoint.
    /// </summary>
    event EventHandler<UdpReceivedEventArgs>? Received;

    /// <summary>
    /// Indicates whether the UDP wrapper is connected and ready to send/receive.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects the UDP client to the specified remote address and port.
    /// </summary>
    /// <param name="address">Remote IP address.</param>
    /// <param name="port">Remote UDP port (default: 21105).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(IPAddress address, int port = 21105, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a datagram to the connected remote endpoint, optionally retrying on transient errors.
    /// </summary>
    /// <param name="data">Payload bytes to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    Task SendAsync(byte[] data, CancellationToken cancellationToken = default, int maxRetries = 3);

    /// <summary>
    /// Stops the receiver loop and closes the underlying UDP client, but allows reconnecting later.
    /// </summary>
    Task StopAsync();
}
