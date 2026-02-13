namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55211 - Curved track R1, r = 14.17” (360 mm) / 30°, 12 pieces / circle.
/// </summary>
public class R1 : Curved
{
    public double ArcInDegree { get; set; } = 30;
    public double RadiusInMm { get; set; } = 360;
}