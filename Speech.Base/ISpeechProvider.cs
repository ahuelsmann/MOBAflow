// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>A synthesis provider. Providers produce WAV audio and never play it.</summary>
public interface ISpeechProvider
{
    /// <summary>Stable configuration identifier.</summary>
    string Id { get; }
    /// <summary>Human-readable provider name.</summary>
    string DisplayName { get; }
    /// <summary>Whether synthesis works without a cloud service.</summary>
    bool IsLocal { get; }
    /// <summary>Returns current voices, option support and availability.</summary>
    Task<SpeechCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>Synthesizes audio or returns a display-safe failure. Caller cancellation propagates.</summary>
    Task<SpeechResult> SynthesizeAsync(SpeechRequest request, CancellationToken cancellationToken = default);
}
