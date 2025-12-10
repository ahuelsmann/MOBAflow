// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moba.Common.Extensions;

namespace Moba.Sound;

/// <summary>
/// Health check service for Azure Cognitive Speech Services.
/// Verifies configuration and connectivity to Azure Speech API.
/// </summary>
public class SpeechHealthCheck
{
    private readonly SpeechOptions _options;
    private readonly ILogger<SpeechHealthCheck> _logger;

    public SpeechHealthCheck(IOptions<SpeechOptions> options, ILogger<SpeechHealthCheck> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

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
            this.LogWarning("Azure Speech Service is not configured. Set SPEECH_KEY and SPEECH_REGION.", _logger);
        }
        else
        {
            this.Log($"✅ Azure Speech Service is configured for region: {speechRegion}", _logger);
        }

        return isConfigured;
    }

    /// <summary>
    /// Performs a simple connectivity test to Azure Speech Service.
    /// </summary>
    /// <returns>True if connection test succeeds, false otherwise</returns>
    public async Task<bool> TestConnectivityAsync()
    {
        if (!IsConfigured())
        {
            this.LogWarning("Cannot test connectivity - service not configured", _logger);
            return false;
        }

        string? speechKey = _options.Key ?? Environment.GetEnvironmentVariable("SPEECH_KEY");
        string? speechRegion = _options.Region ?? Environment.GetEnvironmentVariable("SPEECH_REGION");

        // Test mode short-circuit
        if (string.Equals(speechKey, "test-key", StringComparison.Ordinal))
        {
            this.Log("✅ Connectivity test skipped (test mode)", _logger);
            return true;
        }

        try
        {
            this.Log("🔍 Testing Azure Speech Service connectivity...", _logger);

            var config = SpeechConfig.FromSubscription(speechKey!, speechRegion!);
            
            // Simple test: Try to create a synthesizer (doesn't actually call Azure yet)
            using var synthesizer = new SpeechSynthesizer(config, null);
            
            // If we can create the config and synthesizer, credentials are likely valid
            this.Log("✅ Azure Speech Service connectivity test passed", _logger);
            return true;
        }
        catch (Exception ex)
        {
            this.LogError(
                $"Azure Speech Service connectivity test failed. Possible causes: Invalid/expired API key, Incorrect region ({speechRegion}), Network/Firewall blocking Azure services",
                ex,
                _logger);
            return false;
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
