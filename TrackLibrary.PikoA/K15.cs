namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55240 - Crossing K15, 15° angle, straight track = G239.
/// </summary>
public class K15 : Crossing
{
    public uint LengthInMm { get; set; } = 239;
    public double ArcInDegree { get; set; } = 15;
}