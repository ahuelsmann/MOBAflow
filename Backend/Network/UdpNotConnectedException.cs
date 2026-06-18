// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Network;

/// <summary>
/// Thrown when a send is attempted on <see cref="UdpWrapper"/> while the client is not connected.
/// Allows callers to handle disconnect/shutdown without matching exception messages.
/// </summary>
public sealed class UdpNotConnectedException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UdpNotConnectedException"/> class.
    /// </summary>
    public UdpNotConnectedException()
        : base("UdpWrapper is not connected")
    {
    }
}