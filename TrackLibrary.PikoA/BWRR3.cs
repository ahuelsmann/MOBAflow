// ReSharper disable All
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55228 - Right hand curved switch.
/// Suitable for passing from radius R3 to R4, the main track's radius of the turnout is R3.
/// </summary>
public sealed record BWRR3 : SwitchCurvedRight
{
    /// <summary>
    /// Gets the arc angle for radius R3 in degrees.
    /// </summary>
    public double ArcInDegreeR3 { get; init; } = 30;

    /// <summary>
    /// Gets the radius for track branch R3 in millimeters.
    /// </summary>
    public double RadiusInMmR3 { get; init; } = 483.75;

    /// <summary>
    /// Gets the arc angle for radius R5 in degrees.
    /// </summary>
    public double ArcInDegreeR5 { get; init; } = 30;

    /// <summary>
    /// Gets the radius for track branch R5 in millimeters.
    /// </summary>
    public double RadiusInMmR5 { get; init; } = 607.51;
}