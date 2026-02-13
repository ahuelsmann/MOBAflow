namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55251 - Curved track R1, r = 14.17” (360 mm) / 7,5°, 48 pieces / circle.
/// </summary>
public class R175 : Curved
{
    public double ArcInDegree { get; set; } = 7.5;
    public double RadiusInMm { get; set; } = 360;
}