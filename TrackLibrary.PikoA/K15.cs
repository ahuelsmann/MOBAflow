namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55240 - Crossing K15, 15° angle, straight track = G239.
/// </summary>
public sealed record K15 : Crossing
{
    /// <summary>
    /// Gets the length of the crossing track in millimeters.
    /// </summary>
    public double LengthInMm { get; init; } = 239.07;

    /// <summary>
    /// Gets the crossing angle in degrees.
    /// </summary>
    public double ArcInDegree { get; init; } = 15;
}
