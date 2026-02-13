namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55212 - Curved track R2, r = 16.61” (421.88 mm) / 30°, 12 pieces / circle.
/// </summary>
public class R2 : Curved
{
    public double ArcInDegree { get; set; } = 30;
    public double RadiusInMm { get; set; } = 421.88;
}