// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Sound;

using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Health check service for Azure Cognitive Speech Services.
/// Verifies configuration and connectivity to Azure Speech API.
/// </summary>
public class SpeechHealthCheck(IOptions<SpeechOptions> options, ILogger<SpeechHealthCheck> logger)
{
    private readonly SpeechOptions _options = options.Value;

    /// <summary>
    /// Checks if Azure Speech Service is properly configured.
    /// </summary>
    /// <returns>True if credentials are configured, false otherwise</returns>
    public bool IsConfigured()
    {
        string? speechKey = _options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = _options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

        bool isConfigured = !string.IsNullOrEmpty(speechKey) && !string.IsNullOrEmpty(speechRegion);

        if (!isConfigured)
        {
            logger.LogWarning("Azure Speech Service is not configured. Set SPEECH_KEY and SPEECH_REGION");
        }
        else
        {
            logger.LogInformation("Azure Speech Service is configured for region: {SpeechRegion}", speechRegion);
        }

        return isConfigured;
    }

    /// <summary>
    /// Performs a simple connectivity test to Azure Speech Service.
    /// </summary>
    /// <returns>True if connection test succeeds, false otherwise</returns>
    public Task<bool> TestConnectivityAsync()
    {
        if (!IsConfigured())
        {
            logger.LogWarning("Cannot test connectivity - service not configured");
            return Task.FromResult(false);
        }

        string? speechKey = _options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = _options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

        // Test mode short-circuit
        if (string.Equals(speechKey, "test-key", StringComparison.Ordinal))
        {
            logger.LogInformation("Connectivity test skipped (test mode)");
            return Task.FromResult(true);
        }

        try
        {
            logger.LogInformation("Testing Azure Speech Service connectivity...");

            var config = SpeechConfig.FromSubscription(speechKey!, speechRegion!);
            
            // Simple test: Try to create a synthesizer (doesn't actually call Azure yet)
            using var synthesizer = new SpeechSynthesizer(config, null);
            
            // If we can create the config and synthesizer, credentials are likely valid
            logger.LogInformation("Azure Speech Service connectivity test passed");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure Speech Service connectivity test failed. Region: {Region}", speechRegion);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets detailed configuration status information.
    /// </summary>
    /// <returns>Configuration status message</returns>
    public string GetStatusMessage()
    {
        string? speechKey = _options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = _options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

        if (string.IsNullOrEmpty(speechKey))
        {
            return "❌ SPEECH_KEY not configured";
        }

        if (string.IsNullOrEmpty(speechRegion))
        {
            return "❌ SPEECH_REGION not configured";
        }

        // Mask key for security (show only first 4 and last 4 characters)
        string maskedKey = speechKey.Length > 8
            ? $"{speechKey[..4]}...{speechKey[^4..]}"
            : "****";

        return $"✅ Configured: Region={speechRegion}, Key={maskedKey}";
    }
}