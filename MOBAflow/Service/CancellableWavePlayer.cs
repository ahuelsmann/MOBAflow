// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Speech.Base;
using System.Media;

/// <summary>A native playback session owned by one announcement.</summary>
internal interface IWavePlaybackSession : IDisposable
{
    /// <summary>Loads and starts asynchronous playback.</summary>
    void Play();
    /// <summary>Stops audible playback immediately.</summary>
    void Stop();
}

/// <summary>Plays WAV audio asynchronously and stops it when the caller cancels.</summary>
internal sealed partial class CancellableWavePlayer(
    Func<ReadOnlyMemory<byte>, IWavePlaybackSession>? createSession = null,
    TimeProvider? timeProvider = null) : IWavePlayer
{
    private readonly Func<ReadOnlyMemory<byte>, IWavePlaybackSession> _createSession = createSession ?? (audio => new NativeSession(audio));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    // WinMM replaces playback process-wide; only the current speech session may stop it.
    private static readonly Lock PlaybackGate = new();
    private static IWavePlaybackSession? _activeSession;

    /// <inheritdoc />
    public async Task PlayAsync(ReadOnlyMemory<byte> waveAudio, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var duration = WaveAudio.GetDuration(waveAudio.Span);
        using var session = _createSession(waveAudio);
        using var registration = cancellationToken.Register(() => StopIfCurrent(session));
        try
        {
            await Task.Run(() => StartSession(session, cancellationToken), cancellationToken).ConfigureAwait(false);
            await Task.Delay(duration, _timeProvider, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            StopIfCurrent(session);
        }
    }

    private static void StartSession(IWavePlaybackSession session, CancellationToken cancellationToken)
    {
        lock (PlaybackGate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            session.Play();
            _activeSession = session;
        }
    }
    private static void StopIfCurrent(IWavePlaybackSession session)
    {
        lock (PlaybackGate)
        {
            if (ReferenceEquals(_activeSession, session))
            {
                session.Stop();
                _activeSession = null;
            }
        }
    }

    private sealed partial class NativeSession(ReadOnlyMemory<byte> audio) : IWavePlaybackSession
    {
        private readonly MemoryStream _stream = new(audio.ToArray(), writable: false);
        private SoundPlayer? _player;

        public void Play()
        {
            _player = new SoundPlayer(_stream);
            _player.Load();
            _player.Play();
        }

        public void Stop() => _player?.Stop();

        public void Dispose()
        {
            _player?.Dispose();
            _stream.Dispose();
        }
    }
}
