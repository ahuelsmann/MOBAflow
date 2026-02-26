namespace Moba.TrackLibrary.Base;

/// <summary>
/// Linksweiche mit drei Ports (A, B, C).
/// </summary>
public abstract record SwitchLeft : Segment
{
    /// <summary>
    /// Connection port A of the left switch.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the left switch.
    /// </summary>
    public Guid? PortB { get; set; }

    /// <summary>
    /// Connection port C of the left switch.
    /// </summary>
    public Guid? PortC { get; set; }
}
