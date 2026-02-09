namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55224 Double Slip Switch DKW 15°/ 9.41” (239 mm) Double slip switch, 15° angle, straight track = G239, turnouts = R9.
/// </summary>
public class DKW : SwitchDoubleCrossover
{
    public uint LengthInMm { get; set; } = 239;
    public double ArcInDegree { get; set; } = 15;
    public int RadiusInMm { get; set; } = 908;
}