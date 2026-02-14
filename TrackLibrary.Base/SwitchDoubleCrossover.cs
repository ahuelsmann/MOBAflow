namespace Moba.TrackLibrary.Base;

/// <summary>
/// Doppelkreuzungsweiche mit vier Ports (A, B, C, D).
/// </summary>
public abstract record SwitchDoubleCrossover : Segment
{
    public Guid? PortA { get; set; }
    public Guid? PortB { get; set; }
    public Guid? PortC { get; set; }
    public Guid? PortD { get; set; }
}
