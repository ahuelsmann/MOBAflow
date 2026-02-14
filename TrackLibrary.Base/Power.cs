namespace Moba.TrackLibrary.Base;

/// <summary>
/// Stromversorgungs-Segment mit zwei Ports (A, B).
/// </summary>
public record Power : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
}
