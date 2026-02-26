namespace Moba.TrackLibrary.Base;

/// <summary>
/// Linkskurvenweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchCurvedLeft : Segment
{
    /// <summary>
    /// Connection port A of the left curved switch.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the left curved switch.
    /// </summary>
    public Guid? PortB { get; set; }

    /// <summary>
    /// Connection port C of the left curved switch.
    /// </summary>
    public Guid? PortC { get; set; }
}
