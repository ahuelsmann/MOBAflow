// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Persistent WAV storage shared by all speech providers.</summary>
public interface IAudioCache
{
    /// <summary>Changes when the cache is cleared; prevents older work from repopulating it.</summary>
    long Generation { get; }
    /// <summary>Reads valid audio, discarding missing or corrupt entries.</summary>
    Task<byte[]?> TryGetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Stores valid audio no larger than the size limit, if the captured generation is still current. Until it has
    /// played, it displaces only other unplayed audio.
    /// </summary>
    Task StoreAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken = default);
    /// <summary>Records successful playback and evicts the least recently played audio beyond the size limit.</summary>
    Task MarkPlayedAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Returns the total size of retained WAV files in bytes.</summary>
    Task<long> GetSizeAsync(CancellationToken cancellationToken = default);
    /// <summary>Clears audio and invalidates writes from work already in progress.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
