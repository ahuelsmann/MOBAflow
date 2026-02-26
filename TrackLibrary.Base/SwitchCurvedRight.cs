namespace Moba.TrackLibrary.Base;

/// <summary>
/// Rechtskurvenweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchCurvedRight : Segment
{
    /// <summary>
    /// Connection port A of the right curved switch.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the right curved switch.
    /// </summary>
    public Guid? PortB { get; set; }

    /// <summary>
    /// Connection port C of the right curved switch.
    /// </summary>
    public Guid? PortC { get; set; }
}
