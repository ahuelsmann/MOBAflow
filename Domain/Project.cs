// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

/// <summary>
/// Project - Pure Data Object.
/// Business logic (validation, persistence) belongs in Application Layer (Backend/Services).
/// </summary>
public class Project
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Project"/> class with empty collections.
    /// </summary>
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
        TripLogEntries = [];
    }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configured speech engines for this project.
    /// </summary>
    public List<SpeakerEngineConfiguration> SpeakerEngines { get; set; }

    /// <summary>
    /// Gets or sets the available voices for this project.
    /// </summary>
    public List<Voice> Voices { get; set; }

    /// <summary>
    /// Gets or sets the locomotives belonging to this project.
    /// </summary>
    public List<Locomotive> Locomotives { get; set; }

    /// <summary>
    /// Gets or sets the passenger wagons belonging to this project.
    /// </summary>
    public List<PassengerWagon> PassengerWagons { get; set; }

    /// <summary>
    /// Gets or sets the goods wagons belonging to this project.
    /// </summary>
    public List<GoodsWagon> GoodsWagons { get; set; }

    /// <summary>
    /// Gets or sets the trains defined in this project.
    /// </summary>
    public List<Train> Trains { get; set; }

    /// <summary>
    /// Gets or sets the workflows available in this project.
    /// </summary>
    public List<Workflow> Workflows { get; set; }

    /// <summary>
    /// Gets or sets the journeys defined in this project.
    /// </summary>
    public List<Journey> Journeys { get; set; }

    /// <summary>
    /// Signal box plan for this project (MOBAesb - Electronic Signal Box).
    /// Topological representation with signals, switches, and routes.
    /// </summary>
    public SignalBoxPlan? SignalBoxPlan { get; set; }

    /// <summary>
    /// Fahrtenbuch: protokollierte Fahrten und Haltezeiten von der TrainControlPage.
    /// </summary>
    public List<TripLogEntry> TripLogEntries { get; set; }
}