// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Moba.Domain.Enum;

/// <summary>
/// Journey - Pure Data Object (POCO).
/// NO BUSINESS LOGIC! Execution logic moved to JourneyService in Backend.
/// </summary>
public class Journey
{
    public Journey()
    {
        Name = "New Journey";
        Description = string.Empty;
        Stations = [];
        Text = string.Empty;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public string Text { get; set; }
    public List<Station> Stations { get; set; }
    public uint InPort { get; set; }
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
    public BehaviorOnLastStop BehaviorOnLastStop { get; set; }

    /// <summary>
    /// Current position tracking (managed by JourneyService)
    /// </summary>
    public int CurrentPos { get; set; }
    
    /// <summary>
    /// Current counter for lap tracking (managed by JourneyService)
    /// </summary>
    public int CurrentCounter { get; set; }
    
    /// <summary>
    /// Callback/Action to execute when last station is reached
    /// </summary>
    public Action? OnLastStop { get; set; }
    
    /// <summary>
    /// Reference to next journey (for chaining journeys)
    /// </summary>
    public Journey? NextJourney { get; set; }
    
    /// <summary>
    /// First position index (default: 0)
    /// </summary>
    public int FirstPos { get; set; }
}
