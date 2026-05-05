// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Rendering;

public static class FrameDimensions
{
    public const int Width = 240;
    public const int Height = 280;
    public const int Columns = 2;
    public const int Rows = 3;
    public const int BytesPerPixel = 2;
    public const int FrameByteCount = Width * Height * BytesPerPixel;
}