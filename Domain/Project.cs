// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using Moba.TrackPlan.Graph;

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
    /// Track layout for this project (MOBAtps - Track Planner System).
    /// Uses the new TopologyGraph-based architecture from TrackPlan project.
    /// </summary>
    public TopologyGraph? TrackPlan { get; set; }

    /// <summary>
    /// Signal box plan for this project (MOBAesb - Electronic Signal Box).
    /// Topological representation with signals, switches, and routes.
    /// </summary>
    public SignalBoxPlan? SignalBoxPlan { get; set; }

    /// <summary>
    /// Track catalogs for this project (Piko A, Roco, Mehano, etc.).
    /// Aggregated catalog system containing all available track systems and components.
    /// Used by TrackPlan editor to access templates for building topologies.
    /// </summary>
    public Catalogs? Catalogs { get; set; }
}