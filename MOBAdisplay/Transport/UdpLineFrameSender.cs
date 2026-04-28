using System.Net;
using System.Net.Sockets;
using System.Text;

using Moba.Display.Rendering;
using Moba.Display.Runtime;

namespace Moba.Display.Transport;

public sealed class UdpLineFrameSender : IDisposable
{
    private static readonly byte[] FrameStart = Encoding.ASCII.GetBytes("FRAME_START");
    private static readonly byte[] FrameDone = Encoding.ASCII.GetBytes("FRAME_DONE");

    private readonly UdpClient _udpClient = new(AddressFamily.InterNetwork);
    private bool _disposed;

    /// <inheritdoc cref="IFrameSender.SendFrameAsync"/>
    public async Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameLoopOptions options,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (options.Transport != DisplayTransportKind.Udp)
        {
            throw new InvalidOperationException("UdpLineFrameSender only supports DisplayTransportKind.Udp.");
        }

        if (rgb565Frame.Length != FrameDimensions.FrameByteCount)
        {
            throw new ArgumentException("RGB565 frame size is invalid.", nameof(rgb565Frame));
        }

        if (!IPAddress.TryParse(options.IpAddress, out var ip))
        {
            throw new ArgumentException("Invalid IP address format.", nameof(options));
        }

        await SendFrameCoreAsync(rgb565Frame, ip, options.Port, cancellationToken).ConfigureAwait(false);
    }

    public void SendFrame(ReadOnlySpan<byte> rgb565Frame, string ipAddress, int port)
    {
        ThrowIfDisposed();
        if (!IPAddress.TryParse(ipAddress, out var ip))
        {
            throw new ArgumentException("Invalid IP address format.", nameof(ipAddress));
        }

        if (rgb565Frame.Length != FrameDimensions.FrameByteCount)
        {
            throw new ArgumentException("RGB565 frame size is invalid.", nameof(rgb565Frame));
        }

        _udpClient.Connect(new IPEndPoint(ip, port));
        _udpClient.Send(FrameStart, FrameStart.Length);

        var bytesPerLine = FrameDimensions.Width * FrameDimensions.BytesPerPixel;
        for (var row = 0; row < FrameDimensions.Height; row++)
        {
            var offset = row * bytesPerLine;
            var lineBytes = rgb565Frame.Slice(offset, bytesPerLine).ToArray();
            _udpClient.Send(lineBytes, lineBytes.Length);
            Thread.Sleep(1);
        }

        _udpClient.Send(FrameDone, FrameDone.Length);
    }

    private async Task SendFrameCoreAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        IPAddress ip,
        int port,
        CancellationToken cancellationToken)
    {
        _udpClient.Connect(new IPEndPoint(ip, port));
        await _udpClient.SendAsync(FrameStart, cancellationToken).ConfigureAwait(false);

        var bytesPerLine = FrameDimensions.Width * FrameDimensions.BytesPerPixel;
        for (var row = 0; row < FrameDimensions.Height; row++)
        {
            var line = rgb565Frame.Slice(row * bytesPerLine, bytesPerLine).ToArray();
            await _udpClient.SendAsync(line, cancellationToken).ConfigureAwait(false);
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }

        await _udpClient.SendAsync(FrameDone, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _udpClient.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
