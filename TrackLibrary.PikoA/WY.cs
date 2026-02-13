namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// 55226 - Y switch, 30° angle, turnouts = R9.
/// </summary>
public class WY : SwitchTwoWay
{
    public double ArcInDegree { get; set; } = 30;
    public double RadiusInMm { get; set; } = 907.97;
}