// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Rendering;

public interface IFrameRenderer
{
    void Render(FrameContext context, Span<byte> destinationRgb565);
}