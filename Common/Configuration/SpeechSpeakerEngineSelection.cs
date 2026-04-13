// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Configuration;

/// <summary>
/// Well-known values for <see cref="SpeechSettings.SpeakerEngineName"/> (settings / UI).
/// Avoids fragile substring checks on localized or renamed display strings.
/// </summary>
public static class SpeechSpeakerEngineSelection
{
    /// <summary>Azure Cognitive Speech (engine id stored in settings).</summary>
    public const string AzureCognitiveServices = "AzureCognitiveServices";

    /// <summary>Windows system SAPI engine (engine id stored in settings).</summary>
    public const string SystemSpeech = "SystemSpeech";

    /// <summary>Legacy default from appsettings / older builds (exact match only).</summary>
    public const string LegacyAzureDisplayName = "Azure Cognitive Services";

    /// <summary>Legacy WinUI menu caption (exact match only).</summary>
    public const string LegacySystemDisplayName = "System Speech (Windows SAPI)";

    /// <summary>
    /// Returns whether the configured name selects Azure Cognitive Speech (with credential check done separately).
    /// </summary>
    public static bool ShouldUseAzureCognitive(string? engineName)
    {
        if (string.IsNullOrWhiteSpace(engineName))
        {
            return false;
        }

        return engineName.Equals(AzureCognitiveServices, StringComparison.OrdinalIgnoreCase)
            || engineName.Equals(LegacyAzureDisplayName, StringComparison.OrdinalIgnoreCase);
    }

}
