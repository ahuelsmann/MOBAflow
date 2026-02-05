// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Configuration options for Azure Cognitive Speech Services.
/// Used with IOptions pattern for dependency injection.
/// </summary>
public class SpeechOptions
{
    /// <summary>
    /// Azure Speech Service subscription key.
    /// Can be set via environment variable SPEECH_KEY.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Azure Speech Service region (e.g., "germanywestcentral").
    /// Can be set via environment variable SPEECH_REGION.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Speech synthesis rate (-10 to 10).
    /// Negative values = slower, positive values = faster, 0 = normal speed.
    /// Default: -1 (slightly slower than normal)
    /// </summary>
    public int Rate { get; set; } = -1;

    /// <summary>
    /// Speech synthesis volume (0-100).
    /// Default: 90
    /// </summary>
    public int Volume { get; set; } = 90;

    /// <summary>
    /// Azure voice name for speech synthesis (e.g., "de-DE-KatjaNeural").
    /// If empty, a default voice will be used.
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>
    /// Selected speaker engine name (e.g., "Azure Cognitive Services", "System Speech (Windows SAPI)").
    /// Determines which TTS engine to use.
    /// </summary>
    public string? SpeakerEngineName { get; set; }

    /// <summary>
    /// Gets whether the speech service is configured with valid credentials.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Region);
}