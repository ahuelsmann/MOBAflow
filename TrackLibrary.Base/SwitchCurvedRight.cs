namespace Moba.TrackLibrary.Base;

/// <summary>
/// Rechtskurvenweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchCurvedRight : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public Guid? PortC { get; set; }
}
