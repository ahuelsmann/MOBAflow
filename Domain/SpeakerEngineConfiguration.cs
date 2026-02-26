// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Configuration for a speech synthesis engine.
/// Pure data object - no logic, no dependencies.
/// </summary>
public class SpeakerEngineConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpeakerEngineConfiguration"/> class with default engine settings.
    /// </summary>
    public SpeakerEngineConfiguration()
    {
        Name = "AzureCognitiveSpeech";
        Type = "Microsoft.CognitiveServices.Speech";
        Settings = [];
    }

    /// <summary>
    /// Gets or sets the unique name of the speech engine configuration.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the speech engine implementation.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the key value settings for the speech engine.
    /// </summary>
    public Dictionary<string, string>? Settings { get; set; }
}