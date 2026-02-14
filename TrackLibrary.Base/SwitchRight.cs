namespace Moba.TrackLibrary.Base;

/// <summary>
/// Rechtsweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchRight : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public Guid? PortC { get; set; }
}
