namespace Moba.Backend.Model;

public class Station
{
    public Station()
    {
        Name = "New Station";
    }

    public string Name { get; set; }
    public uint Number { get; set; }
    public uint NumberOfLapsToStop { get; set; }
    public DateTime? Arrival { get; set; }
    public DateTime? Departure { get; set; }
    public uint Track { get; set; }
    public bool IsExitOnLeft { get; set; }
    public Workflow? Flow { get; set; }
}