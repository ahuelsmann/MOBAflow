namespace Moba.TrackLibrary.Base;

/// <summary>
/// Entkuppler-Segment mit zwei Ports (A, B).
/// </summary>
public record Uncoupler : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
}
