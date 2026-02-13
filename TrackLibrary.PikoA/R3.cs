namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55213 - Curved track R3, r = 19.05” (483.75 mm) / 30°, 12 pieces / circle.
/// </summary>
public class R3 : Curved
{
    public double ArcInDegree { get; set; } = 30;
    public double RadiusInMm { get; set; } = 483.75;
}