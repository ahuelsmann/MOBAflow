namespace Moba.TrackLibrary.Base;

/// <summary>
/// Linksweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchLeft : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public Guid? PortC { get; set; }
}
