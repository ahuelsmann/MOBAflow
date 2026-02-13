namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55225 Three Way Switch W3 15° & 15°/ R9 Three way switch, 2 x 15° angle, straight track = G239, turnouts = R9.
/// </summary>
public class W3 : SwitchThreeWay
{
    public uint LengthInMm { get; set; } = 239;
    public double ArcInDegree { get; set; } = 30;
    public double RadiusInMm { get; set; } = 907.97;
}