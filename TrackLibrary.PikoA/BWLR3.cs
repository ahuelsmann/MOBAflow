namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55227 - Left hand curved switch.
/// Suitable for passing from radius R3 to R4, the main track’s radius of the turnout is R3.
/// </summary>
public class BWLR3 : SwitchCurvedLeft
{
    public double ArcInDegreeR3 { get; set; } = 30;
    public double RadiusInMmR3 { get; set; } = 483.75;
    public double ArcInDegreeR5 { get; set; } = 30;
    public double RadiusInMmR5 { get; set; } = 607.6;
}