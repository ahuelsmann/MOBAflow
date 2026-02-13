namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55220 - Left hand switch, 15° angle, straight track = G239, turnout = R9
/// </summary>
public class WL : SwitchLeft
{
    public uint LengthInMm { get; set; } = 239;
    public double ArcInDegree { get; set; } = 15;
    public double RadiusInMm { get; set; } = 907.97;
}