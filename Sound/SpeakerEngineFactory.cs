// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Factory for creating speaker engines based on configuration.
/// Allows dynamic switching between Azure Cognitive Services and Windows SAPI.
/// Uses AppSettings for engine selection to support runtime changes via UI.
/// </summary>
public class SpeakerEngineFactory
{
    private readonly AppSettings _appSettings;
    private readonly IOptionsMonitor<SpeechOptions> _optionsMonitor;
    private readonly ILogger<CognitiveSpeechEngine> _azureLogger;
    private readonly ILogger<SystemSpeechEngine> _systemLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeakerEngineFactory"/>.
    /// </summary>
    /// <param name="appSettings">Global application settings including speech configuration.</param>
    /// <param name="optionsMonitor">Options monitor for Azure Speech configuration.</param>
    /// <param name="azureLogger">Logger instance for the Azure-based speech engine.</param>
    /// <param name="systemLogger">Logger instance for the Windows system speech engine.</param>
    public SpeakerEngineFactory(
        AppSettings appSettings,
        IOptionsMonitor<SpeechOptions> optionsMonitor,
        ILogger<CognitiveSpeechEngine> azureLogger,
        ILogger<SystemSpeechEngine> systemLogger)
    {
        _appSettings = appSettings;
        _optionsMonitor = optionsMonitor;
        _azureLogger = azureLogger;
        _systemLogger = systemLogger;
    }

    /// <summary>
    /// Creates the appropriate speaker engine based on current configuration.
    /// </summary>
    /// <param name="engineName">Engine name (e.g., "Azure Cognitive Services" or "System Speech (Windows SAPI)")</param>
    /// <returns>Configured speaker engine</returns>
    public ISpeakerEngine CreateEngine(string engineName)
    {
        _systemLogger.LogDebug("🔊 [FACTORY] Creating engine for: '{EngineName}'", engineName);

        if (!string.IsNullOrEmpty(engineName) &&
            engineName.Contains("Azure", StringComparison.OrdinalIgnoreCase))
        {
            // Check if Azure credentials are available
            var options = _optionsMonitor.CurrentValue;
            var speechKey = options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
            var speechRegion = options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

            if (!string.IsNullOrWhiteSpace(speechKey) && !string.IsNullOrWhiteSpace(speechRegion))
            {
                _systemLogger.LogDebug("🔊 [FACTORY] ✅ Creating CognitiveSpeechEngine");
                return new CognitiveSpeechEngine(_optionsMonitor, _azureLogger);
            }
            else
            {
                // Fallback to Windows SAPI if credentials are missing
                _systemLogger.LogWarning("🔊 [FACTORY] ⚠️ Azure Speech selected but credentials missing. Falling back to Windows SAPI.");
            }
        }

        // Default: Windows SAPI
        _systemLogger.LogDebug("🔊 [FACTORY] ✅ Creating SystemSpeechEngine");
        return new SystemSpeechEngine(_systemLogger);
    }

    /// <summary>
    /// Creates the appropriate speaker engine based on current AppSettings.
    /// Uses AppSettings.Speech.SpeakerEngineName which is updated when user changes engine in UI.
    /// </summary>
    public ISpeakerEngine CreateEngineFromOptions()
    {
        // ✅ FIX: Use AppSettings.Speech.SpeakerEngineName instead of SpeechOptions
        // This allows runtime engine switching via UI
        var engineName = _appSettings.Speech.SpeakerEngineName;
        _systemLogger.LogDebug("🔊 [FACTORY] AppSettings.Speech.SpeakerEngineName = '{EngineName}'", engineName);
        return CreateEngine(engineName);
    }
}
