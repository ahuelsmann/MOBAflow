namespace Moba.Backend.Model;

using Moba.Backend.Converter;

using Newtonsoft.Json;

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

    [JsonConverter(typeof(WorkflowConverter))]
    public Workflow? Flow { get; set; }
}