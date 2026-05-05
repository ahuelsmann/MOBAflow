// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Runtime;

public sealed class FrameLoopOptions
{
    /// <summary>Wi-Fi target ESP32 address (UDP).</summary>
    public string IpAddress { get; init; } = "192.168.0.82";

    /// <summary>UDP destination port.</summary>
    public int Port { get; init; } = 4210;

    public int RefreshHz { get; init; } = 10;

    public int Width { get; init; } = Rendering.FrameDimensions.Width;

    public int Height { get; init; } = Rendering.FrameDimensions.Height;

    public bool SendDisplayMetadata { get; init; }
}