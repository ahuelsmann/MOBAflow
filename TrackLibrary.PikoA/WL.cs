// ReSharper disable All
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55220 - Left hand switch, 15° angle, straight track = G239, turnout = R9.
/// </summary>
public sealed record WL : SwitchLeft
{
    /// <summary>
    /// Gets the straight length of the switch in millimeters.
    /// </summary>
    public double LengthInMm { get; init; } = 239.07;

    /// <summary>
    /// Gets the turnout angle in degrees.
    /// </summary>
    public double ArcInDegree { get; init; } = 15;

    /// <summary>
    /// Gets the turnout radius in millimeters.
    /// </summary>
    public double RadiusInMm { get; init; } = 907.97;
}