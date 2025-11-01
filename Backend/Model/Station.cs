namespace Moba.Backend.Model;

using Converter;

using Newtonsoft.Json;

/// <summary>
/// Represents a stop.
/// </summary>
public class Station
{
    public Station()
    {
        Name = "New Station";
        Platforms = new List<Platform>();
    }

    public string Name { get; set; }
    public uint Number { get; set; }
    public uint NumberOfLapsToStop { get; set; }
    public DateTime? Arrival { get; set; }
    public DateTime? Departure { get; set; }
    public uint Track { get; set; }
    public bool IsExitOnLeft { get; set; }

    /// <summary>
    /// Workflow triggered by journey progression when the train reaches this station.
    /// </summary>
    [JsonConverter(typeof(WorkflowConverter))]
    public Workflow? Flow { get; set; }

    /// <summary>
    /// List of platforms associated with this station.
    /// Each platform can have its own feedback-triggered workflow for platform-specific announcements.
    /// </summary>
    public List<Platform> Platforms { get; set; }
}