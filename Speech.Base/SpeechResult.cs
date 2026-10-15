// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Expected synthesis failure categories.</summary>
public enum SpeechFailureKind
{
    /// <summary>The provider cannot currently synthesize.</summary>
    Unavailable,
    /// <summary>The text or requested options are invalid.</summary>
    InvalidRequest,
    /// <summary>Synthesis or playback failed.</summary>
    Failed
}

/// <summary>WAV bytes on success, or an expected failure with a display-safe reason.</summary>
public sealed class SpeechResult
{
    private SpeechResult(ReadOnlyMemory<byte> waveAudio, SpeechFailureKind? failureKind, string reason, bool isCacheable)
    {
        WaveAudio = waveAudio;
        FailureKind = failureKind;
        Reason = reason;
        IsCacheable = isCacheable;
    }

    /// <summary>Generated WAV data, empty on failure or when speech is disabled.</summary>
    public ReadOnlyMemory<byte> WaveAudio { get; }
    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess => FailureKind is null;
    /// <summary>Expected failure category, or null on success.</summary>
    public SpeechFailureKind? FailureKind { get; }
    /// <summary>Display-safe explanation, empty on success.</summary>
    public string Reason { get; }
    /// <summary>
    /// Whether the audio may be cached. False when the provider could not honor the requested options, for example a
    /// missing voice, so restoring them later synthesizes again instead of replaying the substitute.
    /// </summary>
    public bool IsCacheable { get; }
    /// <summary>Creates a successful, cacheable result.</summary>
    public static SpeechResult Success(byte[] waveAudio) => Success(waveAudio, isCacheable: true);
    /// <summary>Creates a successful result that may be marked as not cacheable.</summary>
    public static SpeechResult Success(byte[] waveAudio, bool isCacheable)
    {
        ArgumentNullException.ThrowIfNull(waveAudio);
        return new SpeechResult(waveAudio, null, string.Empty, isCacheable);
    }
    /// <summary>Creates a failure without audio.</summary>
    public static SpeechResult Failure(SpeechFailureKind kind, string reason) => new(ReadOnlyMemory<byte>.Empty, kind, reason, isCacheable: false);
}
