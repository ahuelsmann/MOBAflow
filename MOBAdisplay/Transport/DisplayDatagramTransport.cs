// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

/// <summary>
/// Abstracts the connected datagram boundary used by the versioned display protocol.
/// </summary>
public interface IDisplayDatagramTransport
{
    /// <summary>
    /// Raised when one complete datagram arrives from the configured display endpoint.
    /// </summary>
    event EventHandler<DisplayDatagramReceivedEventArgs>? DatagramReceived;

    /// <summary>
    /// Sends exactly one datagram to the configured display endpoint.
    /// </summary>
    /// <param name="datagram">Complete protocol datagram.</param>
    /// <param name="cancellationToken">Stops the pending send.</param>
    ValueTask SendAsync(
        ReadOnlyMemory<byte> datagram,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Carries an immutable copy of one received display datagram.
/// </summary>
public sealed class DisplayDatagramReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes the event data and copies the supplied datagram.
    /// </summary>
    /// <param name="datagram">Complete received datagram.</param>
    public DisplayDatagramReceivedEventArgs(ReadOnlyMemory<byte> datagram)
    {
        Datagram = datagram.ToArray();
    }

    /// <summary>
    /// Gets the immutable received datagram.
    /// </summary>
    public ReadOnlyMemory<byte> Datagram { get; }
}