namespace Moba.Backend.Model;

public class Voice
{
    public Voice()
    {
        Name = "ElkeNeural";
    }

    public string Name { get; set; }
    public decimal ProsodyRate { get; set; }
}