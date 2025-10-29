namespace Moba.Backend.Model;

using Enum;

/// <summary>
/// Represents a journey or timetable with all stops or stations.
/// </summary>
public class Journey
{
    // Lightweight event for notifying changes (no UI dependency!)
    public event EventHandler? StateChanged;

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

    private uint _currentCounter;
    public uint CurrentCounter
    {
        get => _currentCounter;
        set
        {
            if (_currentCounter != value)
            {
                _currentCounter = value;
                StateChanged?.Invoke(this, EventArgs.Empty);  // ✅ Simple event, no UI dependency
            }
        }
    }

    private uint _currentPos;
    public uint CurrentPos
    {
        get => _currentPos;
        set
        {
            if (_currentPos != value)
            {
                _currentPos = value;
                StateChanged?.Invoke(this, EventArgs.Empty);  // ✅ Simple event, no UI dependency
            }
        }
    }

    #region BehaviorOnLastStop
    public BehaviorOnLastStop OnLastStop { get; set; }

    public string? NextJourney { get; set; }

    public uint FirstPos { get; set; }
    #endregion
}