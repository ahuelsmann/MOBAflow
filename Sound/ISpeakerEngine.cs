// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Interface for text-to-speech engines.
/// Implementations include Azure Cognitive Services, Windows System Speech, and NullSpeakerEngine.
/// </summary>
public interface ISpeakerEngine
{
    /// <summary>
    /// Gets or sets the display name of the speech engine.
    /// Used for identification in configuration and logging.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Speaks the given message using the specified voice.
    /// </summary>
    /// <param name="message">Text to synthesize and speak.</param>
    /// <param name="voiceName">Voice identifier (e.g., "de-DE-KatjaNeural" for Azure, null for system default).</param>
    /// <returns>Task that completes when the announcement has finished playing.</returns>
    Task AnnouncementAsync(string message, string? voiceName);
}