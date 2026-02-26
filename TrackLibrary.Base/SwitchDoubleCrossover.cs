namespace Moba.TrackLibrary.Base;

/// <summary>
/// Doppelkreuzungsweiche mit vier Ports (A, B, C, D).
/// </summary>
public abstract record SwitchDoubleCrossover : Segment
{
    /// <summary>
    /// Connection port A of the double crossover switch.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the double crossover switch.
    /// </summary>
    public Guid? PortB { get; set; }

    /// <summary>
    /// Connection port C of the double crossover switch.
    /// </summary>
    public Guid? PortC { get; set; }

    /// <summary>
    /// Connection port D of the double crossover switch.
    /// </summary>
    public Guid? PortD { get; set; }
}
