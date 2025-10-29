namespace Moba.Backend.Model;

using Enum;

/// <summary>
/// Represents a train consisting of locomotives and wagons.
/// </summary>
public class Train
{
    public Train()
    {
        Name = "New Train";
        Wagons = [];
        Locomotives = [];
        TrainType = TrainType.None;
        ServiceType = ServiceType.None;
    }

    public bool IsDoubleTraction { get; set; }
    public string Name { get; set; }
    public TrainType TrainType { get; set; }
    public ServiceType ServiceType { get; set; }
    public List<Locomotive> Locomotives { get; set; }
    public List<Wagon> Wagons { get; set; }
}