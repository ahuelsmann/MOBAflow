namespace Moba.Backend.Model;

using Moba.Backend.Model.Enum;

public class Details
{
    public byte Axles { get; set; }
    public Epoch? Epoch { get; set; }
    public string? RailroadCompany { get; set; }
    public PowerSystem? Power { get; set; }
    public DigitalSystem? Digital { get; set; }
    public string? Description { get; set; }
}