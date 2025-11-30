// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;



/// <summary>
/// Configuration for a speech synthesis engine.
/// Pure data object - no logic, no dependencies.
/// </summary>
public class SpeakerEngineConfiguration
{
    public SpeakerEngineConfiguration()
    {
        Name = "AzureCognitiveSpeech";
        Type = "Microsoft.CognitiveServices.Speech";
        Settings = new Dictionary<string, string>();
    }

    public string Name { get; set; }

    public string Type { get; set; }

    public Dictionary<string, string>? Settings { get; set; }
}
