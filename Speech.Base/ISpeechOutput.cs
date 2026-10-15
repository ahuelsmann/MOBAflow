// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Shared announcement output; resolves current settings for each call.</summary>
public interface ISpeechOutput
{
    /// <summary>Synthesizes and plays text, or returns a safe failure without interrupting a workflow.</summary>
    Task<SpeechResult> SpeakAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>Plays synthesized WAV data with cancellation throughout audible playback.</summary>
public interface IWavePlayer
{
    /// <summary>
    /// Completes when playback finishes; cancellation stops audible playback. Throws
    /// <see cref="WavePlaybackInterruptedException"/> when a newer playback replaced this one before it finished.
    /// </summary>
    Task PlayAsync(ReadOnlyMemory<byte> waveAudio, CancellationToken cancellationToken = default);
}
