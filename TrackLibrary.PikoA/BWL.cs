// ReSharper disable All
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55222 - Left hand curved switch.
/// Suitable for passing from radius R2 to R3 or R3 to R4, the main track's radius of the turnout is R2.
/// </summary>
public sealed record BWL : SwitchCurvedLeft
{
    /// <summary>
    /// Gets the arc angle for radius R2 in degrees.
    /// </summary>
    public double ArcInDegreeR2 { get; init; } = 30;

    /// <summary>
    /// Gets the radius for track branch R2 in millimeters.
    /// </summary>
    public double RadiusInMmR2 { get; init; } = 421.88;

    /// <summary>
    /// Gets the arc angle for radius R3 in degrees.
    /// </summary>
    public double ArcInDegreeR3 { get; init; } = 30;

    /// <summary>
    /// Gets the radius for track branch R3 in millimeters.
    /// </summary>
    public double RadiusInMmR3 { get; init; } = 483.75;
}