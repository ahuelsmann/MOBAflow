namespace Moba.TrackLibrary.Base;

/// <summary>
/// Gerades Gleissegment mit zwei Ports (A, B) und fester Länge.
/// </summary>
public abstract record Straight(double LengthInMm) : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
}
