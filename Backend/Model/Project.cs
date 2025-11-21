// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Model;

using Sound;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a MOBA project. All elements necessary for the application are defined here.
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
        IpAddresses = [];
        Settings = new Settings();
        Cities = [];
    }

    [Display(Name = "Project Name")]
    public string Name { get; set; } = string.Empty;

    public List<CognitiveSpeechEngine> SpeakerEngines { get; set; }
    public List<Voice> Voices { get; set; }
    public List<Locomotive> Locomotives { get; set; }
    public List<PassengerWagon> PassengerWagons { get; set; }
    public List<GoodsWagon> GoodsWagons { get; set; }
    public List<Train> Trains { get; set; }
    public List<Workflow> Workflows { get; set; }
    public List<Journey> Journeys { get; set; }
    
    /// <summary>
    /// DEPRECATED: IP addresses have been moved to Solution.Settings.IpAddresses.
    /// This property is kept for backward compatibility during JSON deserialization.
    /// </summary>
    [Obsolete("Use Solution.Settings.IpAddresses instead. This property is only used for migration from old JSON files.")]
    public List<string> IpAddresses { get; set; }
    
    /// <summary>
    /// DEPRECATED: Settings have been moved to Solution.Settings.
    /// This property is kept for backward compatibility during JSON deserialization.
    /// </summary>
    [Obsolete("Use Solution.Settings instead. This property is only used for migration from old JSON files.")]
    public Settings Settings { get; set; }
    
    /// <summary>
    /// List of cities with their stations available for adding to journeys.
    /// </summary>
    public List<Data.City> Cities { get; set; }
}