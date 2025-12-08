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
        Id = Guid.NewGuid();
        Name = "New Journey";
        Description = string.Empty;
        JourneyStations = [];
        Text = string.Empty;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Text { get; set; }
    
    /// <summary>
    /// Junction entities linking this Journey to Stations with Journey-specific properties.
    /// Each JourneyStation contains StationId + IsExitOnLeft + WorkflowId + NumberOfLapsToStop.
    /// Example: Journey1 can have "Hauptbahnhof" with IsExitOnLeft=true,
    ///          Journey2 can have same station with IsExitOnLeft=false.
    /// </summary>
    public List<JourneyStation> JourneyStations { get; set; }
    
    public uint InPort { get; set; } = 1;
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }
    public BehaviorOnLastStop BehaviorOnLastStop { get; set; }

    /// <summary>
    /// Reference to next journey ID (for chaining journeys)
    /// Used when BehaviorOnLastStop == GotoJourney
    /// </summary>
    public Guid? NextJourneyId { get; set; }

    /// <summary>
    /// First position index (default: 0)
    /// </summary>
    public uint FirstPos { get; set; }
}