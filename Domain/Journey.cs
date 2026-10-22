// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// An ordered list of stops with its announcement text and feedback events, and the rules for moving between its stops.
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

    /// <summary>
    /// Finds the index of the current stop: the stop with <paramref name="currentStationId"/> when one is known,
    /// otherwise <paramref name="fallbackPosition"/>.
    /// </summary>
    /// <returns>The stop index, or a value outside <see cref="Stations"/> when the stop is unknown.</returns>
    public int IndexOfStop(Guid? currentStationId, int fallbackPosition) =>
        currentStationId.HasValue
            ? Stations.FindIndex(station => station.Id == currentStationId.Value)
            : fallbackPosition;

    /// <summary>
    /// Resolves the stop that <paramref name="transition"/> leads to from the stop at <paramref name="currentIndex"/>.
    /// Moving past the last stop keeps the current stop; an unknown current stop moves to the first stop.
    /// </summary>
    /// <returns>The index of the target stop, or <see langword="null"/> when the current stop stays.</returns>
    /// <exception cref="InvalidOperationException">The transition names a station that is not a stop of this journey.</exception>
    public int? ResolveStopTransition(int currentIndex, JourneyStopTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        if (transition.Mode == JourneyStopTransitionMode.None)
            return null;

        var targetIndex = transition.Mode == JourneyStopTransitionMode.Next
            ? currentIndex + 1
            : Stations.FindIndex(station => station.Id == transition.StationId);

        if (targetIndex >= Stations.Count && transition.Mode == JourneyStopTransitionMode.Next)
            return null;

        if (targetIndex < 0 || targetIndex >= Stations.Count)
            throw new InvalidOperationException("The configured target stop does not exist in the current journey");

        return targetIndex;
    }
}
