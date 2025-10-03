namespace Moba.Backend.Model;

using Sound;

public class Project
{
    public Project()
    {
        SpeakerEngines = [];
        Voices = [];
        Locomotives = [];
        PassengerWagons = [];
        GoodsWagons = [];
        Trains = [];
        Workflows = [];
        Journeys = [];
        Ips = [];
        Setting = new Setting();
    }

    public List<SpeakerEngine> SpeakerEngines { get; set; }
    public List<Voice> Voices { get; set; }
    public List<Locomotive> Locomotives { get; set; }
    public List<PassengerWagon> PassengerWagons { get; set; }
    public List<GoodsWagon> GoodsWagons { get; set; }
    public List<Train> Trains { get; set; }
    public List<Workflow> Workflows { get; set; }
    public List<Journey> Journeys { get; set; }
    public List<Ip> Ips { get; set; }
    public Setting Setting { get; set; }
}