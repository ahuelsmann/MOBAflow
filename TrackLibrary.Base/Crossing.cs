namespace Moba.TrackLibrary.Base;

/// <summary>
/// Kreuzung mit vier Ports (A, B, C, D).
/// </summary>
public abstract record Crossing : Segment
{
    /// <summary>
    /// Connection port A of the crossing.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the crossing.
    /// </summary>
    public Guid? PortB { get; set; }

    /// <summary>
    /// Connection port C of the crossing.
    /// </summary>
    public Guid? PortC { get; set; }

    /// <summary>
    /// Connection port D of the crossing.
    /// </summary>
    public Guid? PortD { get; set; }
}
