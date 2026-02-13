namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55214 - Curved track R4, r = 21.48” (545.63 mm) / 30°, 12 pieces / circle.
/// </summary>
public class R4 : Curved
{
    public double ArcInDegree { get; set; } = 30;
    public double RadiusInMm { get; set; } = 545.63;
}