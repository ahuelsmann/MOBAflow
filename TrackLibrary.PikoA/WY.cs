// ReSharper disable All
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55226 - Y switch, 30° angle, turnouts = R9.
/// </summary>
public sealed record WY : SwitchTwoWay
{
    /// <summary>
    /// Gets the turnout angle in degrees.
    /// </summary>
    public double ArcInDegree { get; init; } = 30;

    /// <summary>
    /// Gets the turnout radius in millimeters.
    /// </summary>
    public double RadiusInMm { get; init; } = 907.97;
}