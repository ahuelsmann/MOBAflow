// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Configuration;

/// <summary>
/// Well-known values for <see cref="SpeechSettings.SpeakerEngineName"/> (settings / UI).
/// Avoids fragile substring checks on localized or renamed display strings.
/// </summary>
public static class SpeechSpeakerEngineSelection
{
    /// <summary>Piper TTS engine (engine id stored in settings).</summary>
    public const string PiperTts = "PiperTts";

    /// <summary>Windows system SAPI engine (engine id stored in settings).</summary>
    public const string SystemSpeech = "SystemSpeech";

    /// <summary>WinUI menu caption for Piper TTS (exact match only).</summary>
    public const string PiperDisplayName = "Piper TTS";

    /// <summary>Legacy WinUI menu caption (exact match only).</summary>
    public const string LegacySystemDisplayName = "System Speech (Windows SAPI)";

    /// <summary>
    /// Returns whether the configured name selects Piper TTS (with path checks done separately).
    /// </summary>
    public static bool ShouldUsePiperTts(string? engineName)
    {
        if (string.IsNullOrWhiteSpace(engineName))
        {
            return false;
        }

        return engineName.Equals(PiperTts, StringComparison.OrdinalIgnoreCase)
            || engineName.Equals(PiperDisplayName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns whether the configured name selects Windows system speech.
    /// </summary>
    public static bool ShouldUseSystemSpeech(string? engineName)
    {
        if (string.IsNullOrWhiteSpace(engineName))
        {
            return true;
        }

        return engineName.Equals(SystemSpeech, StringComparison.OrdinalIgnoreCase)
            || engineName.Equals(LegacySystemDisplayName, StringComparison.OrdinalIgnoreCase);
    }

}
