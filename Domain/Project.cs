// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Project - Pure Data Object.
/// Business logic (validation, persistence) belongs in Application Layer (Backend/Services).
/// </summary>
public class Project
{
    public Project()
    {
        SpeakerEngines = new List<SpeakerEngineConfiguration>();
        Voices = new List<Voice>();
        Locomotives = new List<Locomotive>();
        PassengerWagons = new List<PassengerWagon>();
        GoodsWagons = new List<GoodsWagon>();
        Trains = new List<Train>();
        Workflows = new List<Workflow>();
        Journeys = new List<Journey>();
        FeedbackPoints = new List<FeedbackPointOnTrack>();
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
    public List<FeedbackPointOnTrack> FeedbackPoints { get; set; }
}