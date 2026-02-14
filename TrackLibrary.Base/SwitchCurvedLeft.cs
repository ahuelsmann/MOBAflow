namespace Moba.TrackLibrary.Base;

/// <summary>
/// Linkskurvenweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchCurvedLeft : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public Guid? PortC { get; set; }
}
