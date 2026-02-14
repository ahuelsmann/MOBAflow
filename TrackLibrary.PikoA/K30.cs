namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55241 - Crossing K30, 30° angle, straight track = G119.
/// </summary>
public sealed record K30 : Crossing
{
    public uint LengthInMm { get; init; } = 119;
    public double ArcInDegree { get; init; } = 30;
}
