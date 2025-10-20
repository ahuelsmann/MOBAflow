namespace Moba.Backend.Model;

using Enum;

public class Train
{
    public Train()
    {
        Name = "New Train";
        Wagons = [];
        Locomotives = [];
        TrainType = TrainType.Passenger;
        ServiceType = ServiceType.None;
    }

    public bool IsDoubleTraction { get; set; }
    public string Name { get; set; } // e.g. RE6
    public TrainType TrainType { get; set; }
    public ServiceType ServiceType { get; set; }
    public List<Locomotive> Locomotives { get; set; }
    public List<Wagon> Wagons { get; set; }
}