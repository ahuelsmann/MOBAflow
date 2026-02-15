namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55228 - Right hand curved switch.
/// Suitable for passing from radius R3 to R4, the main track's radius of the turnout is R3.
/// </summary>
public sealed record BWRR3 : SwitchCurvedRight
{
    public double ArcInDegreeR3 { get; init; } = 30;
    public double RadiusInMmR3 { get; init; } = 483.75;
    public double ArcInDegreeR5 { get; init; } = 30;
    public double RadiusInMmR5 { get; init; } = 607.51;
}
