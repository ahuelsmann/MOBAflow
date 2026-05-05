// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Net.Sockets;
using System.Text;

namespace Moba.Display;

public static class FrameSender
{
    private const int Port = 4210;

    public static void SendFrame(byte[] rgb565Frame, string ip)
    {
        ArgumentNullException.ThrowIfNull(rgb565Frame);
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        using var client = new UdpClient();
        client.Connect(ip, Port);

        const int chunkSize = 1024;

        for (var i = 0; i < rgb565Frame.Length; i += chunkSize)
        {
            var size = Math.Min(chunkSize, rgb565Frame.Length - i);
            client.Send(rgb565Frame.AsSpan(i, size).ToArray());
            Thread.Sleep(1);
        }

        var end = Encoding.ASCII.GetBytes("FRAME_DONE");
        client.Send(end);
    }
}