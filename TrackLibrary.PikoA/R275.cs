namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55252 - Curved track R2, r = 16.61” (421.88 mm) / 7,5°, 48 pieces / circle.
/// </summary>
public class R275 : Curved
{
    public double ArcInDegree { get; set; } = 7.5;
    public double RadiusInMm { get; set; } = 421.88;
}