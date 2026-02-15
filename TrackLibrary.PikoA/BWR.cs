namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55223 - Right hand curved switch.
/// Suitable for passing from radius R2 to R3 or R3 to R4, the main track's radius of the turnout is R2.
/// </summary>
public sealed record BWR : SwitchCurvedRight
{
    public double ArcInDegreeR2 { get; init; } = 30;
    public double RadiusInMmR2 { get; init; } = 421.88;
    public double ArcInDegreeR3 { get; init; } = 30;
    public double RadiusInMmR3 { get; init; } = 483.75;
}
