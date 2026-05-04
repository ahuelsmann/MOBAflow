namespace Moba.Display.Transport;

using Moba.Display.Runtime;

/// <summary>
/// Sends RGB565 frames to the MOBAdisplay device.
/// </summary>
public interface IFrameSender
{
    Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameLoopOptions options,
        CancellationToken cancellationToken = default);
}