namespace Moba.TrackLibrary.Base;

/// <summary>
/// Sensor-Segment mit zwei Ports (A, B).
/// </summary>
public abstract record Sensor : Segment
{
    /// <summary>
    /// Connection port A of the sensor segment.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the sensor segment.
    /// </summary>
    public Guid? PortB { get; set; }
}
