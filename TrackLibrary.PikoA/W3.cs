// ReSharper disable All
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55225 Three Way Switch W3 15° &amp; 15°/ R9 Three way switch, 2 x 15° angle, straight track = G239, turnouts = R9.
/// </summary>
public sealed record W3 : SwitchThreeWay
{
    public double LengthInMm { get; init; } = 239.07;
    public double ArcInDegree { get; init; } = 30;
    public double RadiusInMm { get; init; } = 907.97;
}
