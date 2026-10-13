// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

/// <summary>Persistent WAV storage shared by all speech providers.</summary>
public interface IAudioCache
{
    /// <summary>Changes when the cache is cleared; prevents older work from repopulating it.</summary>
    long Generation { get; }
    /// <summary>Reads valid audio, discarding missing or corrupt entries.</summary>
    Task<byte[]?> TryGetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Stores valid audio within the size limit, if the captured generation is still current.</summary>
    Task StoreAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken = default);
    /// <summary>Records successful playback for least-recently-played eviction.</summary>
    Task MarkPlayedAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Returns the total size of retained WAV files in bytes.</summary>
    Task<long> GetSizeAsync(CancellationToken cancellationToken = default);
    /// <summary>Clears audio and invalidates writes from work already in progress.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
