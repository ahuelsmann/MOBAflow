namespace Moba.Backend.Model;

using Enum;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class Journey : ObservableObject
{
    public Journey()
    {
        Name = "New Journey";
        Text = string.Empty;
        NextJourney = string.Empty;
        OnLastStop = BehaviorOnLastStop.None;
        Stations = [];
    }

    [ObservableProperty]
    private uint inPort;

    [ObservableProperty]
    private bool isUsingTimerToIgnoreFeedbacks;

    [ObservableProperty]
    private double intervalForTimerToIgnoreFeedbacks;

    [ObservableProperty]
    private string name = "New Journey";

    [ObservableProperty]
    private string? text = string.Empty;

    [ObservableProperty]
    private Train? train;

    public List<Station> Stations { get; set; }

    [ObservableProperty]
    private uint currentCounter;

    [ObservableProperty]
    private uint currentPos;

    #region BehaviorOnLastStop
    [ObservableProperty]
    private BehaviorOnLastStop onLastStop;

    [ObservableProperty]
    private string? nextJourney = string.Empty;

    [ObservableProperty]
    private uint firstPos;
    #endregion
}