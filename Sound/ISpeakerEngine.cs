// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Interface for text-to-speech engines.
/// Implementations include Piper TTS, Windows System Speech, and NullSpeakerEngine.
/// </summary>
public interface ISpeakerEngine
{
    /// <summary>
    /// Gets the display name of the speech engine.
    /// Used for identification in configuration and logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Speaks the given message using the specified voice.
    /// </summary>
    /// <param name="message">Text to synthesize and speak.</param>
    /// <param name="voiceName">Voice identifier, or <c>null</c> for the configured/default voice.</param>
    /// <returns>Task that completes when the announcement has finished playing.</returns>
    Task AnnouncementAsync(string message, string? voiceName);

    /// <summary>
    /// Speaks the given message and observes cancellation during synthesis and playback.
    /// </summary>
    /// <param name="message">Text to synthesize and speak.</param>
    /// <param name="voiceName">Voice identifier, or <c>null</c> for the configured/default voice.</param>
    /// <param name="cancellationToken">Cancellation token for synthesis and playback.</param>
    Task AnnouncementAsync(string message, string? voiceName, CancellationToken cancellationToken);
}
