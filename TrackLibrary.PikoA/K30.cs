namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55241 - Crossing K30, 30° angle, straight track = G119.
/// </summary>
public sealed record K30 : Crossing
{
    /// <summary>
    /// Gets the length of the crossing track in millimeters.
    /// </summary>
    public double LengthInMm { get; init; } = 119.54;

    /// <summary>
    /// Gets the crossing angle in degrees.
    /// </summary>
    public double ArcInDegree { get; init; } = 30;
}
