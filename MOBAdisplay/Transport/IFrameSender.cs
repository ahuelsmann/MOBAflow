namespace Moba.Display.Transport;

public interface IFrameSender
{
    Task SendFrameAsync(ReadOnlyMemory<byte> rgb565Frame, string ipAddress, int port, CancellationToken cancellationToken = default);
}
