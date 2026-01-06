// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Journey - Pure Data Object (POCO).
/// </summary>
public class Journey
{
    public Journey()
    {
        Id = Guid.NewGuid();
        Name = "New Journey";
        Description = string.Empty;
        Stations = [];
        Text = string.Empty;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Text { get; set; }

    public List<Station> Stations { get; set; }

    public uint InPort { get; set; }
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