namespace Moba.Backend.Model;

using Moba.Backend.Model.Enum;

public class Journey
{
    public Journey()
    {
        Name = "New Journey";
        Text = string.Empty;
        NextJourney = string.Empty;
        OnLastStop = BehaviorOnLastStop.None;
        Stations = [];
    }

    public uint InPort { get; set; }
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
    public string Name { get; set; }
    public string? Text { get; set; }
    public Train? Train { get; set; }
    public List<Station> Stations { get; set; }
    public uint CurrentCounter { get; set; }
    public uint CurrentPos { get; set; }

    #region BehaviorOnLastStop
    public BehaviorOnLastStop OnLastStop { get; set; }
    public string? NextJourney { get; set; }
    public uint FirstPos { get; set; }
    #endregion
}