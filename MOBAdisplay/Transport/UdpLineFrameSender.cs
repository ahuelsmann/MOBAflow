// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Display.Rendering;
using Moba.Display.Runtime;

using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Moba.Display.Transport;

public sealed class UdpLineFrameSender : IFrameSender, IDisposable
{
    private static readonly byte[] FrameStart = Encoding.ASCII.GetBytes("FRAME_START");
    private static readonly byte[] FrameDone = Encoding.ASCII.GetBytes("FRAME_DONE");
    private static readonly byte[] HostVersionPacket = Encoding.ASCII.GetBytes($"HOST_VER:{ResolveHostVersion()}");

    private readonly UdpClient _udpClient = new(AddressFamily.InterNetwork);
    private string _lastEndpoint = string.Empty;
    private bool _hostVersionSentForEndpoint;
    private bool _disposed;

    /// <inheritdoc cref="IFrameSender.SendFrameAsync"/>
    public async Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameLoopOptions options,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var frameByteCount = options.Width * options.Height * FrameDimensions.BytesPerPixel;
        if (rgb565Frame.Length != frameByteCount)
        {
            throw new ArgumentException("RGB565 frame size is invalid.", nameof(rgb565Frame));
        }

        if (!IPAddress.TryParse(options.IpAddress, out var ip))
        {
            throw new ArgumentException("Invalid IP address format.", nameof(options));
        }

        await SendFrameCoreAsync(rgb565Frame, ip, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendFrameCoreAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        IPAddress ip,
        FrameLoopOptions options,
        CancellationToken cancellationToken)
    {
        var endpointKey = $"{ip}:{options.Port}";
        if (!string.Equals(_lastEndpoint, endpointKey, StringComparison.Ordinal))
        {
            _lastEndpoint = endpointKey;
            _hostVersionSentForEndpoint = false;
        }

        _udpClient.Connect(new IPEndPoint(ip, options.Port));
        if (!_hostVersionSentForEndpoint)
        {
            await _udpClient.SendAsync(HostVersionPacket, cancellationToken).ConfigureAwait(false);
            _hostVersionSentForEndpoint = true;
        }
        await _udpClient.SendAsync(FrameStart, cancellationToken).ConfigureAwait(false);

        var bytesPerLine = options.Width * FrameDimensions.BytesPerPixel;
        var packet = new byte[bytesPerLine + 2];
        for (var row = 0; row < options.Height; row++)
        {
            packet[0] = (byte)((row >> 8) & 0xFF);
            packet[1] = (byte)(row & 0xFF);
            rgb565Frame.Slice(row * bytesPerLine, bytesPerLine).Span.CopyTo(packet.AsSpan(2));
            await _udpClient.SendAsync(packet, cancellationToken).ConfigureAwait(false);
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }

        await _udpClient.SendAsync(FrameDone, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveHostVersion()
    {
        var assembly = typeof(UdpLineFrameSender).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational!;
        }

        return assembly.GetName().Version?.ToString() ?? "dev";
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