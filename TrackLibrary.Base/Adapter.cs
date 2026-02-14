namespace Moba.TrackLibrary.Base;

/// <summary>
/// Adapter-Segment mit zwei Ports (A, B).
/// </summary>
public abstract record Adapter : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
}
