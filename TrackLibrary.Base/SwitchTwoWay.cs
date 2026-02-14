namespace Moba.TrackLibrary.Base;

/// <summary>
/// Weiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchTwoWay : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public Guid? PortC { get; set; }
}
