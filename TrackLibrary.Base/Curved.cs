namespace Moba.TrackLibrary.Base;

/// <summary>
/// Kurvensegment mit zwei Ports (A, B) und fester Geometrie (Radius, Bogenwinkel).
/// </summary>
public abstract record Curved(double ArcInDegree, double RadiusInMm) : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
}
