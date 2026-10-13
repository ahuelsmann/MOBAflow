// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Persistent WAV storage shared by all speech providers.</summary>
public interface IAudioCache
{
    /// <summary>Changes when the cache is cleared; prevents older work from repopulating it.</summary>
    long Generation { get; }
    /// <summary>
    /// Reads valid audio, discarding missing or corrupt entries. The generation is read in the same locked operation,
    /// so a miss can be stored later only if no clear happened since this lookup.
    /// </summary>
    Task<AudioCacheLookup> TryGetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Stores audio that has just played successfully, if the captured generation is still current, and evicts the
    /// least recently played audio beyond the size limit. Audio larger than the limit is not stored.
    /// </summary>
    Task StoreAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken = default);
    /// <summary>Records successful playback of cached audio for least-recently-played eviction.</summary>
    Task MarkPlayedAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Returns the total size of retained WAV files in bytes.</summary>
    Task<long> GetSizeAsync(CancellationToken cancellationToken = default);
    /// <summary>Clears audio and invalidates writes from work already in progress.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>Cached audio, or null on a miss, and the cache generation read with it.</summary>
public readonly record struct AudioCacheLookup(ReadOnlyMemory<byte>? Audio, long Generation);
