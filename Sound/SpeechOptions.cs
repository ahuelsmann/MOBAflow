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
    /// Gets whether the speech service is configured with valid credentials.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Region);
}