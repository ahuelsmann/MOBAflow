namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55219 - Curved track for switch R9, r = 35.75” (907.97 mm) / 15°, 24 pieces / circle.
/// </summary>
public class R9 : Curved
{
    public double ArcInDegree { get; set; } = 15;
    public double RadiusInMm { get; set; } = 907.97;
}