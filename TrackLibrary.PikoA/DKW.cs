namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55224 Double Slip Switch DKW 15°/ 9.41" (239 mm) Double slip switch, 15° angle, straight track = G239, turnouts = R9.
/// </summary>
public sealed record DKW : SwitchDoubleCrossover
{
    public double LengthInMm { get; init; } = 239.07;
    public double ArcInDegree { get; init; } = 15;
    public double RadiusInMm { get; init; } = 907.97;
}
