namespace Moba.TrackLibrary.Base;

/// <summary>
/// Rechtsweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchRight : Segment
{
    /// <summary>
    /// Connection port A of the right switch.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the right switch.
    /// </summary>
    public Guid? PortB { get; set; }

    /// <summary>
    /// Connection port C of the right switch.
    /// </summary>
    public Guid? PortC { get; set; }
}
