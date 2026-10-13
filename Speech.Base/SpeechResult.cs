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
    private SpeechResult(ReadOnlyMemory<byte> waveAudio, SpeechFailureKind? failureKind, string reason)
    {
        WaveAudio = waveAudio;
        FailureKind = failureKind;
        Reason = reason;
    }

    /// <summary>Generated WAV data, empty on failure or when speech is disabled.</summary>
    public ReadOnlyMemory<byte> WaveAudio { get; }
    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess => FailureKind is null;
    /// <summary>Expected failure category, or null on success.</summary>
    public SpeechFailureKind? FailureKind { get; }
    /// <summary>Display-safe explanation, empty on success.</summary>
    public string Reason { get; }
    /// <summary>Creates a successful result.</summary>
    public static SpeechResult Success(byte[] waveAudio)
    {
        ArgumentNullException.ThrowIfNull(waveAudio);
        return new SpeechResult(waveAudio, null, string.Empty);
    }
    /// <summary>Creates a failure without audio.</summary>
    public static SpeechResult Failure(SpeechFailureKind kind, string reason) => new(ReadOnlyMemory<byte>.Empty, kind, reason);
}
