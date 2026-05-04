namespace Moba.Display.Rendering;

public interface IFrameRenderer
{
    void Render(FrameContext context, Span<byte> destinationRgb565);
}