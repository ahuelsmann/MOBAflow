namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55226 - Y switch, 30° angle, turnouts = R9.
/// </summary>
public sealed record WY : SwitchTwoWay
{
    public double ArcInDegree { get; init; } = 30;
    public double RadiusInMm { get; init; } = 907.97;
}
