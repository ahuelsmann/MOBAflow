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