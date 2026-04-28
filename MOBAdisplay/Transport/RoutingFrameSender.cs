using Moba.Display.Runtime;

namespace Moba.Display.Transport;

/// <summary>
/// Dispatches frames to UDP or USB‑serial transmission and releases the COM port when using UDP only.
/// </summary>
public sealed class RoutingFrameSender : IFrameSender, IDisposable
{
    private readonly UdpLineFrameSender _udp;
    private readonly SerialLineFrameSender _serial;

    public RoutingFrameSender(UdpLineFrameSender udpLineFrameSender, SerialLineFrameSender serialLineFrameSender)
    {
        _udp = udpLineFrameSender ?? throw new ArgumentNullException(nameof(udpLineFrameSender));
        _serial = serialLineFrameSender ?? throw new ArgumentNullException(nameof(serialLineFrameSender));
    }

    /// <inheritdoc />
    public async Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameLoopOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Transport == DisplayTransportKind.Serial)
        {
            await _serial.SendFrameAsync(rgb565Frame, options, cancellationToken).ConfigureAwait(false);
            return;
        }

        _serial.ReleasePort();
        await _udp.SendFrameAsync(rgb565Frame, options, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _serial.Dispose();
        _udp.Dispose();
    }
}
