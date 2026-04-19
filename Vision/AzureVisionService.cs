// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Vision;

using Azure;
using Azure.AI.Vision.ImageAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// Azure AI Vision Image Analysis implementation of <see cref="IVisionService"/>.
/// Exposes the "Read" (OCR) visual feature.
/// </summary>
/// <remarks>
/// Configuration via <see cref="VisionOptions"/> or environment variables:
/// <list type="bullet">
/// <item><description><c>VISION_KEY</c>: Azure AI Vision subscription key</description></item>
/// <item><description><c>VISION_ENDPOINT</c>: Azure AI Vision endpoint URL
/// (e.g. <c>https://myaivision-xxx.cognitiveservices.azure.com/</c>)</description></item>
/// </list>
/// Uses the <c>test-key</c> sentinel for unit tests to avoid calling Azure.
/// </remarks>
public class AzureVisionService : IVisionService
{
    private const string TestSentinelKey = "test-key";

    private readonly IOptionsMonitor<VisionOptions>? _optionsMonitor;
    private readonly ILogger<AzureVisionService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AzureVisionService"/> for production use with DI.
    /// </summary>
    public AzureVisionService(IOptionsMonitor<VisionOptions> optionsMonitor, ILogger<AzureVisionService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance for design-time or serialization scenarios.
    /// </summary>
    public AzureVisionService()
    {
        _optionsMonitor = null;
        _logger = NullLogger<AzureVisionService>.Instance;
    }

    /// <inheritdoc />
    public string Name => "Azure.AI.Vision.ImageAnalysis";

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            var options = _optionsMonitor?.CurrentValue ?? new VisionOptions();
            var key = GetEffectiveKey(options);
            var endpoint = GetEffectiveEndpoint(options);
            return !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(endpoint);
        }
    }

    /// <inheritdoc />
    public async Task<VisionReadResult> ReadTextAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            throw new ArgumentException("Image path is required.", nameof(imagePath));
        }
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("Image file not found.", imagePath);
        }

        await using var fs = File.OpenRead(imagePath);
        return await ReadTextAsync(fs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<VisionReadResult> ReadTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        var options = _optionsMonitor?.CurrentValue ?? new VisionOptions();
        var key = GetEffectiveKey(options);
        var endpoint = GetEffectiveEndpoint(options);

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(endpoint))
        {
            _logger.LogError(
                "Azure AI Vision credentials not configured. Please set VISION_KEY and VISION_ENDPOINT");
            throw new InvalidOperationException(
                "Please configure Azure AI Vision credentials via VisionOptions or environment variables VISION_KEY and VISION_ENDPOINT.\n" +
                "You can set them in Windows:\n" +
                "  setx VISION_KEY \"your-key-here\"\n" +
                "  setx VISION_ENDPOINT \"https://<resource>.cognitiveservices.azure.com/\"");
        }

        // Test short-circuit for unit tests: skip the Azure call entirely.
        if (string.Equals(key, TestSentinelKey, StringComparison.Ordinal))
        {
            _logger.LogInformation("Vision ReadTextAsync (test mode) – returning empty result");
            await Task.Yield();
            return VisionReadResult.Empty;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new InvalidOperationException(
                $"Azure AI Vision endpoint is not a valid absolute URL: '{endpoint}'");
        }

        // Read the stream into memory once so the SDK can send it as a body payload.
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }
            await imageStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            bytes = ms.ToArray();
        }

        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("Image stream is empty.");
        }

        try
        {
            var client = new ImageAnalysisClient(endpointUri, new AzureKeyCredential(key));

            _logger.LogInformation(
                "Calling Azure AI Vision ReadAsync ({Bytes} bytes, endpoint={Endpoint})",
                bytes.Length, endpointUri);

            var response = await client.AnalyzeAsync(
                    BinaryData.FromBytes(bytes),
                    VisualFeatures.Read,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Map(response.Value);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Azure AI Vision request failed. Status={Status}, ErrorCode={ErrorCode}, Endpoint={Endpoint}",
                ex.Status, ex.ErrorCode, endpointUri);
            throw new InvalidOperationException(
                $"Azure AI Vision request failed ({ex.Status} {ex.ErrorCode}): {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Azure AI Vision unexpected failure. Endpoint={Endpoint}", endpointUri);
            throw;
        }
    }

    private static VisionReadResult Map(ImageAnalysisResult result)
    {
        var width = result.Metadata?.Width ?? 0;
        var height = result.Metadata?.Height ?? 0;

        if (result.Read is null || result.Read.Blocks.Count == 0)
        {
            return new VisionReadResult(Array.Empty<VisionReadLine>(), 0, width, height);
        }

        var lines = new List<VisionReadLine>();
        foreach (var block in result.Read.Blocks)
        {
            foreach (var line in block.Lines)
            {
                var words = new List<VisionReadWord>(line.Words.Count);
                foreach (var w in line.Words)
                {
                    words.Add(new VisionReadWord(
                        Text: w.Text ?? string.Empty,
                        Confidence: (float)w.Confidence,
                        BoundingPolygon: MapPolygon(w.BoundingPolygon)));
                }

                lines.Add(new VisionReadLine(
                    Text: line.Text ?? string.Empty,
                    Words: words,
                    BoundingPolygon: MapPolygon(line.BoundingPolygon)));
            }
        }

        return new VisionReadResult(lines, 0, width, height);
    }

    private static IReadOnlyList<VisionPoint> MapPolygon(IEnumerable<ImagePoint>? points)
    {
        if (points is null) return Array.Empty<VisionPoint>();
        var list = new List<VisionPoint>(4);
        foreach (var p in points)
        {
            list.Add(new VisionPoint(p.X, p.Y));
        }
        return list;
    }

    private static string? GetEffectiveKey(VisionOptions options)
    {
        var key = options.Key;
        if (string.IsNullOrEmpty(key))
        {
            key = Environment.GetEnvironmentVariable("VISION_KEY");
        }
        return key;
    }

    private static string? GetEffectiveEndpoint(VisionOptions options)
    {
        var endpoint = options.Endpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
        }
        return endpoint;
    }
}
