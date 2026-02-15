namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55221 - Right hand switch, 15° angle, straight track = G239, turnout = R9.
/// </summary>
public sealed record WR : SwitchRight
{
    public double LengthInMm { get; init; } = 239.07;
    public double ArcInDegree { get; init; } = 15;
    public double RadiusInMm { get; init; } = 907.97;
}
