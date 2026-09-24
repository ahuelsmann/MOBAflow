// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

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
        EventPlan = new JourneyEventPlan();
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
    /// Gets or sets whether the runtime evaluates this journey's events on incoming feedback.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the feedback events, each matched against the InPort session counter.
    /// </summary>
    public JourneyEventPlan EventPlan { get; set; }
}
