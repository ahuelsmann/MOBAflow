namespace Moba.Backend.Model;

public class Voice
{
    /// <summary>
    /// Represents a specific voice for speech output.
    /// </summary>
    public Voice()
    {
        Name = "ElkeNeural";
    }

    public string Name { get; set; }
    public decimal ProsodyRate { get; set; }
}