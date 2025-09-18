namespace Moba.Backend.Model;

public class Train
{
    public Train()
    {
        Name = "New Train";
        Wagons = [];
        Locomotives = [];
    }

    public bool IsDoubleTraction { get; set; }
    public string Name { get; set; } // e.g. RE6
    public List<Locomotive> Locomotives { get; set; }
    public List<Wagon> Wagons { get; set; }
}