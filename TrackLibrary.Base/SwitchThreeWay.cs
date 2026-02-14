namespace Moba.TrackLibrary.Base;

/// <summary>
/// Dreiwegeweiche mit vier Ports (A, B, C, D).
/// </summary>
public abstract record SwitchThreeWay : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public Guid? PortC { get; set; }
    public Guid? PortD { get; set; }
}
