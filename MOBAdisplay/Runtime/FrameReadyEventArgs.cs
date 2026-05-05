// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Runtime;

public sealed class FrameReadyEventArgs : EventArgs
{
    public FrameReadyEventArgs(DateTime timestamp, ReadOnlyMemory<byte> frame)
    {
        Timestamp = timestamp;
        Frame = frame;
    }

    public DateTime Timestamp { get; }
    public ReadOnlyMemory<byte> Frame { get; }
}