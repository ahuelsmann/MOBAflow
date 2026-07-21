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
        Id = Guid.NewGuid();
        Locomotives = [];
        PassengerWagons = [];
        GoodsWagons = [];
        Trains = [];
        Matrices = [];
        Workflows = [];
        Journeys = [];
        Stations = [];
        TimetableServices = [];
        LocomotiveWhistleRules = [];
    }

    /// <summary>Gets or sets the stable project identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

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
    /// Gets or sets the 5x5 matrix images defined in this project.
    /// </summary>
    public List<MatrixImage> Matrices { get; set; }

    /// <summary>
    /// Gets or sets the workflows available in this project.
    /// </summary>
    public List<Workflow> Workflows { get; set; }

    /// <summary>
    /// Gets or sets the journeys defined in this project.
    /// </summary>
    public List<Journey> Journeys { get; set; }

    /// <summary>
    /// Gets or sets the stations defined in this project.
    /// </summary>
    public List<Station> Stations { get; set; }

    /// <summary>
    /// Gets or sets the dated timetable service definitions for this project.
    /// </summary>
    public List<TimetableService> TimetableServices { get; set; }

    /// <summary>
    /// Gets or sets project-wide timetable validation policy.
    /// </summary>
    public TimetablePolicy TimetablePolicy { get; set; } = new();

    /// <summary>
    /// Optional feedback-triggered locomotive function rules.
    /// Empty by default so projects created before the feature remain compatible.
    /// </summary>
    public List<LocomotiveWhistleRule> LocomotiveWhistleRules { get; set; }

    /// <summary>
    /// Signal box plan - Topological representation with signals, switches, and routes.
    /// </summary>
    public SignalBoxPlan? SignalBoxPlan { get; set; }

    /// <summary>
    /// Physical track plan for this project (Piko A-Gleis segments + port connections).
    /// </summary>
    public TrackPlanDocument? TrackPlan { get; set; }

}
