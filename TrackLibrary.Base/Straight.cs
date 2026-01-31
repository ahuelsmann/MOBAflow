namespace Moba.TrackLibrary.Base;

public class Straight : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public uint LengthInMm { get; set; }
}