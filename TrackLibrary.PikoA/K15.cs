namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55240 - Crossing K15, 15° angle, straight track = G239.
/// </summary>
public sealed record K15 : Crossing
{
    public double LengthInMm { get; init; } = 239.07;
    public double ArcInDegree { get; init; } = 15;
}
