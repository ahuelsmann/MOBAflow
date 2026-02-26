namespace Moba.TrackLibrary.Base;

/// <summary>
/// Dreiwegeweiche mit vier Ports (A, B, C, D).
/// </summary>
public abstract record SwitchThreeWay : Segment
{
    /// <summary>
    /// Connection port A of the three-way switch.
    /// </summary>
    public Guid? PortA { get; set; }

    /// <summary>
    /// Connection port B of the three-way switch.
    /// </summary>
    public Guid? PortB { get; set; }

    /// <summary>
    /// Connection port C of the three-way switch.
    /// </summary>
    public Guid? PortC { get; set; }

    /// <summary>
    /// Connection port D of the three-way switch.
    /// </summary>
    public Guid? PortD { get; set; }
}
