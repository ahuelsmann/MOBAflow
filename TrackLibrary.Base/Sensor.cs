namespace Moba.TrackLibrary.Base;

/// <summary>
/// Sensor-Segment mit zwei Ports (A, B).
/// </summary>
public abstract record Sensor : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
}
