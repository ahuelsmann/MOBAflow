namespace Moba.TrackLibrary.Base;

/// <summary>
/// Endstück mit einem Port (A).
/// </summary>
public record End : Segment
{
    /// <summary>
    /// Connection port A of the end segment.
    /// </summary>
    public Guid? PortA { get; set; }
}
