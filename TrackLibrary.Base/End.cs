namespace Moba.TrackLibrary.Base;

/// <summary>
/// End piece with one port (A).
/// </summary>
public record End : Segment
{
    /// <summary>
    /// Connection port A of the end segment.
    /// </summary>
    public Guid? PortA { get; set; }
}
