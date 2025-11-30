// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Model;

/// <summary>
/// Configuration for a speech synthesis engine.
/// Stores only configuration data, not the actual engine implementation.
/// This allows the domain model to remain independent of infrastructure concerns (Dependency Inversion Principle).
/// </summary>
public class SpeakerEngineConfiguration
{
    public SpeakerEngineConfiguration()
    {
        Name = "AzureCognitiveSpeech";
        Type = "Microsoft.CognitiveServices.Speech";
        Settings = new Dictionary<string, string>();
    }

    /// <summary>
    /// Display name of the speaker engine (e.g., "Azure Cognitive Speech")
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type identifier for the engine implementation (e.g., "Microsoft.CognitiveServices.Speech", "System.Speech")
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Optional configuration settings specific to the engine type
    /// </summary>
    public Dictionary<string, string>? Settings { get; set; }
}
