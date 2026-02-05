// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Factory for creating speaker engines based on configuration.
/// Allows dynamic switching between Azure Cognitive Services and Windows SAPI.
/// </summary>
public class SpeakerEngineFactory
{
    private readonly IOptionsMonitor<SpeechOptions> _optionsMonitor;
    private readonly ILogger<CognitiveSpeechEngine> _azureLogger;
    private readonly ILogger<SystemSpeechEngine> _systemLogger;

    public SpeakerEngineFactory(
        IOptionsMonitor<SpeechOptions> optionsMonitor,
        ILogger<CognitiveSpeechEngine> azureLogger,
        ILogger<SystemSpeechEngine> systemLogger)
    {
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
        if (!string.IsNullOrEmpty(engineName) &&
            engineName.Contains("Azure", StringComparison.OrdinalIgnoreCase))
        {
            // Check if Azure credentials are available
            var options = _optionsMonitor.CurrentValue;
            var speechKey = options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
            var speechRegion = options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

            if (!string.IsNullOrWhiteSpace(speechKey) && !string.IsNullOrWhiteSpace(speechRegion))
            {
                return new CognitiveSpeechEngine(_optionsMonitor, _azureLogger);
            }
            else
            {
                // Fallback to Windows SAPI if credentials are missing
                _systemLogger.LogWarning("Azure Speech selected but credentials missing. Falling back to Windows SAPI.");
            }
        }

        // Default: Windows SAPI
        return new SystemSpeechEngine(_systemLogger);
    }

    /// <summary>
    /// Creates the appropriate speaker engine based on current SpeechOptions.
    /// </summary>
    public ISpeakerEngine CreateEngineFromOptions()
    {
        var options = _optionsMonitor.CurrentValue;
        return CreateEngine(options.SpeakerEngineName ?? string.Empty);
    }
}
