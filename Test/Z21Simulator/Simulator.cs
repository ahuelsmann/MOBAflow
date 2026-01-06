// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Z21Simulator;

using System.Net;
using System.Net.Sockets;

public class Simulator : IDisposable
{
    private readonly UdpClient _server;
    private readonly int _port;
    private bool _disposed;

    public IPEndPoint LocalEndpoint => new IPEndPoint(IPAddress.Loopback, _port);

    public Simulator(int port = 21105)
    {
        _port = port;
        _server = new UdpClient(_port);
    }

    public void Send(byte[] data)
    {
        Send(data, LocalEndpoint);
    }

    public void Send(byte[] data, IPEndPoint destination)
    {
        using var client = new UdpClient();
        client.Send(data, data.Length, destination);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _server.Dispose();
        _disposed = true;
    }
}
