namespace Moba.TrackLibrary.Base;

/// <summary>
/// Kurvensegment mit zwei Ports (A, B) und fester Geometrie (Radius, Bogenwinkel).
/// </summary>
public abstract record Curved(double ArcInDegree, double RadiusInMm) : Segment
{
    /// <summary>
    /// Connection port A of the curved segment.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the curved segment.
    /// </summary>
    public Guid? PortB { get; set; }
}
