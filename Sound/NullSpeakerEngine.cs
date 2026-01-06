// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

/// <summary>
/// Null Object implementation of ISpeakerEngine.
/// Used on platforms without text-to-speech support (e.g., WebApp, headless servers).
/// Silently ignores all TTS requests.
/// </summary>
public class NullSpeakerEngine : ISpeakerEngine
{
    public string Name { get; set; } = "Null Speaker Engine (No Audio)";

    /// <summary>
    /// Does nothing. Text-to-speech is not supported on this platform.
    /// </summary>
    public Task AnnouncementAsync(string message, string? voiceName)
    {
        // Silent no-op - platform doesn't support TTS
        return Task.CompletedTask;
    }
}
