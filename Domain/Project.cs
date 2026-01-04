// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.TrackPlan.Domain;

namespace Moba.Domain;

/// <summary>
/// Project - Pure Data Object.
/// Business logic (validation, persistence) belongs in Application Layer (Backend/Services).
/// </summary>
public class Project
{
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
    }

    public string Name { get; set; } = string.Empty;

    public List<SpeakerEngineConfiguration> SpeakerEngines { get; set; }
    public List<Voice> Voices { get; set; }
    public List<Locomotive> Locomotives { get; set; }
    public List<PassengerWagon> PassengerWagons { get; set; }
    public List<GoodsWagon> GoodsWagons { get; set; }
    public List<Train> Trains { get; set; }
    public List<Workflow> Workflows { get; set; }
    public List<Journey> Journeys { get; set; }

    /// <summary>
    /// Track layout for this project (segments, connections, work surface size).
    /// </summary>
    public TrackLayout? TrackLayout { get; set; }
}