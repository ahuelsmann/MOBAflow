// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Utilities;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Utility for checking network port availability.
/// </summary>
internal static class PortChecker
{
    /// <summary>
    /// Checks if a TCP port is available for binding.
    /// </summary>
    /// <param name="port">The port number to check.</param>
    /// <returns>True if the port is available (not in use), false otherwise.</returns>
    public static bool IsPortAvailable(int port)
    {
        try
        {
            // Try to bind to the port - if successful, the port is available
            using var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            // Port is already in use
            return false;
        }
        catch (Exception)
        {
            // Some other error occurred - assume port is not available
            return false;
        }
    }

    /// <summary>
    /// Gets a user-friendly description of the port availability status.
    /// </summary>
    /// <param name="port">The port number to check.</param>
    /// <returns>A status message describing the port's availability.</returns>
    public static string GetPortStatus(int port)
    {
        return IsPortAvailable(port)
            ? $"Port {port} is available"
            : $"Port {port} is already in use by another application";
    }
}
