namespace Moba.TrackLibrary.Base;

/// <summary>
/// Stromversorgungs-Segment mit zwei Ports (A, B).
/// </summary>
public record Power : Segment
{
    /// <summary>
    /// Connection port A of the power feed segment.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the power feed segment.
    /// </summary>
    public Guid? PortB { get; set; }
}
