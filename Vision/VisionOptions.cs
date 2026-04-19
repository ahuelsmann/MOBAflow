// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Vision;

/// <summary>
/// Configuration options for the Azure AI Vision (Image Analysis) service.
/// Used with the <c>IOptions</c>/<c>IOptionsMonitor</c> pattern for dependency injection.
/// </summary>
/// <remarks>
/// Can be populated via appsettings.json (section <c>Vision</c>) or environment variables
/// <c>VISION_KEY</c> and <c>VISION_ENDPOINT</c>.
/// </remarks>
public class VisionOptions
{
    /// <summary>
    /// Azure AI Vision subscription key.
    /// Falls back to the environment variable <c>VISION_KEY</c> when empty.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Azure AI Vision endpoint URL, e.g.
    /// <c>https://myaivision-xxx.cognitiveservices.azure.com/</c>.
    /// Falls back to the environment variable <c>VISION_ENDPOINT</c> when empty.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Optional image file path used by the Settings "Test Vision" button.
    /// </summary>
    public string? TestImagePath { get; set; }

    /// <summary>
    /// Gets whether the vision service is configured with valid credentials.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Endpoint);
}
