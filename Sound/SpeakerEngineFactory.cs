// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Sound;

using Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Factory that selects the appropriate ISpeakerEngine based on AppSettings.
/// Enables runtime switching between SystemSpeechEngine and CognitiveSpeechEngine.
/// </summary>
public class SpeakerEngineFactory : ISpeakerEngineFactory
{
    private readonly AppSettings _settings;
    private readonly SystemSpeechEngine _systemEngine;
    private readonly CognitiveSpeechEngine _cognitiveEngine;
    private readonly ILogger<SpeakerEngineFactory> _logger;

    public SpeakerEngineFactory(
        AppSettings settings,
        SystemSpeechEngine systemEngine,
        CognitiveSpeechEngine cognitiveEngine,
        ILogger<SpeakerEngineFactory> logger)
    {
        _settings = settings;
        _systemEngine = systemEngine;
        _cognitiveEngine = cognitiveEngine;
        _logger = logger;
    }

    /// <summary>
    /// Gets the currently configured speaker engine based on Settings.Speech.SpeakerEngineName.
    /// </summary>
    public ISpeakerEngine GetSpeakerEngine()
    {
        var selectedEngine = _settings.Speech.SpeakerEngineName;

        // Check if Azure Cognitive Services is selected
        if (!string.IsNullOrEmpty(selectedEngine) &&
            selectedEngine.Contains("Azure", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Using Azure Cognitive Speech Engine");
            return _cognitiveEngine;
        }

        // Default to System Speech (Windows SAPI)
        _logger.LogDebug("Using System Speech Engine (Windows SAPI)");
        return _systemEngine;
    }
}
