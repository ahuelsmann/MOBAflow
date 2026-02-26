namespace Moba.TrackLibrary.Base;

/// <summary>
/// Adapter-Segment mit zwei Ports (A, B).
/// </summary>
public abstract record Adapter : Segment
{
    /// <summary>
    /// Connection port A of the adapter segment.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the adapter segment.
    /// </summary>
    public Guid? PortB { get; set; }
}
