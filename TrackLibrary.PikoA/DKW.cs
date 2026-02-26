// ReSharper disable All
namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55224 Double Slip Switch DKW 15°/ 9.41" (239 mm) Double slip switch, 15° angle, straight track = G239, turnouts = R9.
/// </summary>
public sealed record DKW : SwitchDoubleCrossover
{
    /// <summary>
    /// Gets the length of the double slip switch in millimeters.
    /// </summary>
    public double LengthInMm { get; init; } = 239.07;

    /// <summary>
    /// Gets the crossing angle in degrees.
    /// </summary>
    public double ArcInDegree { get; init; } = 15;

    /// <summary>
    /// Gets the turnout radius in millimeters.
    /// </summary>
    public double RadiusInMm { get; init; } = 907.97;
}