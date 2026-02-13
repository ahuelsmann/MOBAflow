namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55241 - Crossing K30, 30° angle, straight track = G119
/// </summary>
public class K30 : Crossing
{
    public uint LengthInMm { get; set; } = 119;
    public double ArcInDegree { get; set; } = 30;
}