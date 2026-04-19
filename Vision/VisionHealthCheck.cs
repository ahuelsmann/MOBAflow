// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Vision;

using Azure;
using Azure.AI.Vision.ImageAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Health check service for Azure AI Vision.
/// Verifies configuration and that a client can be created with the given credentials.
/// </summary>
/// <remarks>
/// The connectivity test does NOT perform an actual API call (would cost quota and needs
/// an image). It only validates the endpoint URL and key shape.
/// </remarks>
public class VisionHealthCheck(IOptions<VisionOptions> options, ILogger<VisionHealthCheck> logger)
{
    private const string TestSentinelKey = "test-key";

    private readonly VisionOptions _options = options.Value;

    /// <summary>
    /// Checks whether key and endpoint are present.
    /// </summary>
    public bool IsConfigured()
    {
        var (key, endpoint) = GetEffectiveCredentials();
        var isConfigured = !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(endpoint);

        if (!isConfigured)
        {
            logger.LogWarning("Azure AI Vision is not configured. Set VISION_KEY and VISION_ENDPOINT");
        }
        else
        {
            logger.LogInformation("Azure AI Vision is configured for endpoint: {Endpoint}", endpoint);
        }
        return isConfigured;
    }

    /// <summary>
    /// Validates that a client can be instantiated with the configured credentials.
    /// Does not make a network call to Azure.
    /// </summary>
    public Task<bool> TestConnectivityAsync()
    {
        if (!IsConfigured())
        {
            logger.LogWarning("Cannot test connectivity - Vision service not configured");
            return Task.FromResult(false);
        }

        var (key, endpoint) = GetEffectiveCredentials();

        // Test short-circuit
        if (string.Equals(key, TestSentinelKey, StringComparison.Ordinal))
        {
            logger.LogInformation("Vision connectivity test skipped (test mode)");
            return Task.FromResult(true);
        }

        try
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
            {
                logger.LogError("Vision endpoint is not a valid absolute URL: {Endpoint}", endpoint);
                return Task.FromResult(false);
            }

            // Creating the client does not call Azure; it only initializes the pipeline.
            _ = new ImageAnalysisClient(endpointUri, new AzureKeyCredential(key!));
            logger.LogInformation("Azure AI Vision connectivity test passed");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure AI Vision connectivity test failed. Endpoint={Endpoint}", endpoint);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Returns a human-readable status message suitable for UI display.
    /// Masks the key for security.
    /// </summary>
    public string GetStatusMessage()
    {
        var (key, endpoint) = GetEffectiveCredentials();
        if (string.IsNullOrEmpty(key))
        {
            return "❌ VISION_KEY not configured";
        }
        if (string.IsNullOrEmpty(endpoint))
        {
            return "❌ VISION_ENDPOINT not configured";
        }

        var maskedKey = key.Length > 8 ? $"{key[..4]}...{key[^4..]}" : "****";
        return $"✅ Configured: Endpoint={endpoint}, Key={maskedKey}";
    }

    private (string? Key, string? Endpoint) GetEffectiveCredentials()
    {
        var key = _options.Key;
        if (string.IsNullOrEmpty(key))
        {
            key = Environment.GetEnvironmentVariable("VISION_KEY");
        }
        var endpoint = _options.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
        }
        return (key, endpoint);
    }
}
