// ReSharper disable All
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55220 - Left hand switch, 15° angle, straight track = G239, turnout = R9.
/// </summary>
public sealed record WL : SwitchLeft
{
    public double LengthInMm { get; init; } = 239.07;
    public double ArcInDegree { get; init; } = 15;
    public double RadiusInMm { get; init; } = 907.97;
}