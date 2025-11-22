// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Net;

namespace Moba.Backend.Network;

/// <summary>
/// Event arguments for UDP packet reception.
/// Contains the received data buffer and the remote endpoint information.
/// </summary>
public class UdpReceivedEventArgs : EventArgs
{
    /// <summary>
    /// The received UDP datagram payload.
    /// </summary>
    public byte[] Buffer { get; }

    /// <summary>
    /// The remote endpoint (IP address and port) that sent the datagram.
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    /// <summary>
    /// Initializes a new instance of the UdpReceivedEventArgs class.
    /// </summary>
    /// <param name="buffer">The received UDP payload</param>
    /// <param name="remote">The sender's endpoint</param>
    public UdpReceivedEventArgs(byte[] buffer, IPEndPoint remote)
    {
        Buffer = buffer;
        RemoteEndPoint = remote;
    }
}