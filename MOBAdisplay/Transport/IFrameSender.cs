// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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