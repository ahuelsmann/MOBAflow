// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Journey - Pure Data Object (POCO).
/// </summary>
public class Journey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Journey"/> class with default values.
    /// </summary>
    public Journey()
    {
        Id = Guid.NewGuid();
        Name = "New Journey";
        Description = string.Empty;
        Stations = [];
        Text = string.Empty;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the journey.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the journey.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a human readable description of the journey.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the announcement text for the journey.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the ordered list of stations in this journey.
    /// </summary>
    public List<Station> Stations { get; set; }

    /// <summary>
    /// Gets or sets the hardware feedback input port used to detect arrival at the first station.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether feedbacks are ignored for a certain time after arrival.
    /// </summary>
    public bool IsUsingTimerToIgnoreFeedbacks { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds for which feedbacks are ignored after arrival.
    /// </summary>
    public double IntervalForTimerToIgnoreFeedbacks { get; set; }

    /// <summary>
    /// Gets or sets the behavior when the last station of the journey is reached.
    /// </summary>
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