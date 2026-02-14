namespace Moba.TrackLibrary.Base;

/// <summary>
/// Endstück mit einem Port (A).
/// </summary>
public record End : Segment
{
    public Guid? PortA { get; set; }
}
