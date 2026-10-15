// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

using System.Text.Json;

/// <summary>
/// Serialized, atomic WAV/index storage with a configurable least-recently-played size limit.
/// A lock file serializes access across processes; every operation reloads the index under that lock.
/// </summary>
public sealed class AudioCache(string directory, Func<long> sizeLimitBytes, TimeProvider clock) : IAudioCache, IDisposable
{
    private const string IndexName = "index.json";
    private const string LockName = "cache.lock";
    private const int LockAttempts = 200;
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(25);
    private readonly string _directory = Path.GetFullPath(directory);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private bool _swept;
    private long _generation;

    /// <inheritdoc />
    public long Generation => Interlocked.Read(ref _generation);

    /// <inheritdoc />
    public async Task<AudioCacheLookup> TryGetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return await UseAsync(async () =>
        {
            // A lowered limit applies before a hit is served; zero disables the cache.
            var changed = Trim(protectedKey: null);
            byte[]? audio = null;
            if (_entries.ContainsKey(key))
            {
                var path = AudioPath(key);
                audio = File.Exists(path)
                    ? await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)
                    : null;
                if (audio is null || !IsValidWave(audio))
                {
                    audio = null;
                    Remove(key);
                    changed = true;
                }
            }
            if (changed) await SaveIndexAsync(CancellationToken.None).ConfigureAwait(false);
            return new AudioCacheLookup(audio is null ? (ReadOnlyMemory<byte>?)null : audio, Generation);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StoreAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        _ = WaveAudio.GetDuration(audio.Span);
        await UseAsync(async () =>
        {
            // Another process may have stopped between writing a WAV and indexing it; stores are rare, so rescan here.
            Sweep(cancellationToken);
            // An existing entry (stored by another waiter or found by a recheck) only records this playback.
            var stored = _entries.ContainsKey(key) && File.Exists(AudioPath(key));
            if (!stored && !await TryWriteNewAudioAsync(key, audio, generation, cancellationToken).ConfigureAwait(false)) return;
            try
            {
                // The WAV is in place: record it even if the caller cancels now.
                var size = stored ? _entries[key].Size : audio.Length;
                _entries[key] = new CacheEntry(size, clock.GetUtcNow());
                Trim(protectedKey: size <= Limit() ? key : null);
                await SaveIndexAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // The WAV may now be unindexed; sweep again under the next lock.
                _swept = false;
                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes new audio only for the current generation and within the limit.</summary>
    private async Task<bool> TryWriteNewAudioAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken)
    {
        if (generation != Generation) return false;
        if (audio.Length > Limit())
        {
            // A lowered limit still applies, although this audio is not retained.
            if (Trim(protectedKey: null)) await SaveIndexAsync(CancellationToken.None).ConfigureAwait(false);
            return false;
        }
        await WriteAtomicAsync(key + ".wav", audio, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task MarkPlayedAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await UseAsync(async () =>
        {
            if (!_entries.TryGetValue(key, out var entry)) return;
            _entries[key] = entry with { LastPlayedAt = clock.GetUtcNow() };
            await SaveIndexAsync(CancellationToken.None).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<long> GetSizeAsync(CancellationToken cancellationToken = default)
    {
        return await UseAsync(async () =>
        {
            // Also count out orphans that another process may have left since the first sweep.
            Sweep(cancellationToken);
            var changed = Reconcile(cancellationToken);
            changed |= Trim(protectedKey: null);
            if (changed) await SaveIndexAsync(CancellationToken.None).ConfigureAwait(false);
            return _entries.Values.Sum(entry => entry.Size);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await UseAsync(async () =>
        {
            // Once deletion starts, finish it and persist the new generation even if the caller cancels.
            Interlocked.Increment(ref _generation);
            foreach (var key in _entries.Keys.ToArray()) Remove(key);
            Sweep(CancellationToken.None);
            await SaveIndexAsync(CancellationToken.None).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task UseAsync(Func<Task> operation, CancellationToken cancellationToken) =>
        await UseAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);

    private async Task<T> UseAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_directory);
            using var processLock = await AcquireProcessLockAsync(cancellationToken).ConfigureAwait(false);
            await LoadAsync(cancellationToken).ConfigureAwait(false);
            return await operation().ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private async Task<FileStream> AcquireProcessLockAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_directory, LockName);
        for (var attempt = 1; attempt < LockAttempts; attempt++)
        {
            try { return OpenLock(path); }
            catch (IOException)
            {
                // Another MOBAflow process holds the cache; its operations take milliseconds.
                await Task.Delay(LockRetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
        // The final attempt reports a lasting lock as IOException.
        return OpenLock(path);
    }

    private static FileStream OpenLock(string path) =>
        new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(_directory, IndexName);
        var index = File.Exists(indexPath) ? await ReadIndexAsync(indexPath, cancellationToken).ConfigureAwait(false) : null;
        _entries = index?.Entries is null
            ? new(StringComparer.Ordinal)
            : new(index.Entries.Where(pair => IsValidKey(pair.Key) && pair.Value is not null && IsPossibleSize(pair.Value.Size)), StringComparer.Ordinal);
        Interlocked.Exchange(ref _generation, Math.Max(Generation, index?.Generation ?? 0));
        if (!_swept) Sweep(cancellationToken);
    }

    private void Sweep(CancellationToken cancellationToken)
    {
        // Writers hold the process lock until their index is saved, so unindexed WAVs and temporary files are leftovers.
        foreach (var path in Directory.EnumerateFiles(_directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(path);
            var key = Path.GetFileNameWithoutExtension(name);
            var orphan = name.EndsWith(".tmp", StringComparison.Ordinal) ||
                (name.EndsWith(".wav", StringComparison.Ordinal) && IsValidKey(key) && !_entries.ContainsKey(key));
            if (orphan) File.Delete(path);
        }
        _swept = true;
    }

    private async Task<CacheIndex?> ReadIndexAsync(string indexPath, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(indexPath, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<CacheIndex>(bytes);
        }
        catch (JsonException)
        {
            // A corrupt index drops all entries; sweep their WAVs again.
            _swept = false;
            return null;
        }
    }

    private bool Reconcile(CancellationToken cancellationToken)
    {
        var changed = false;
        foreach (var key in _entries.Keys.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(AudioPath(key));
            if (!info.Exists)
            {
                _entries.Remove(key);
                changed = true;
            }
            else if (_entries[key].Size != info.Length)
            {
                _entries[key] = _entries[key] with { Size = info.Length };
                changed = true;
            }
        }
        return changed;
    }

    private bool Trim(string? protectedKey)
    {
        var limit = Limit();
        if (_entries.Values.Sum(entry => entry.Size) <= limit) return false;
        // Over the limit: count only WAVs that still exist before evicting valid audio.
        var changed = Reconcile(CancellationToken.None);
        var size = _entries.Values.Sum(entry => entry.Size);
        foreach (var (key, entry) in _entries
            .Where(pair => !string.Equals(pair.Key, protectedKey, StringComparison.Ordinal))
            .OrderBy(pair => pair.Value.LastPlayedAt)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray())
        {
            if (size <= limit) break;
            Remove(key);
            size -= entry.Size;
            changed = true;
        }
        return changed;
    }

    private long Limit() => Math.Max(0, sizeLimitBytes());

    private void Remove(string key)
    {
        File.Delete(AudioPath(key));
        _entries.Remove(key);
    }

    private Task SaveIndexAsync(CancellationToken cancellationToken) =>
        WriteAtomicAsync(IndexName, JsonSerializer.SerializeToUtf8Bytes(new CacheIndex(Generation, _entries)), cancellationToken);

    private async Task WriteAtomicAsync(string name, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_directory, name);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes.ToArray(), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally { File.Delete(temporary); }
    }

    private string AudioPath(string key) => Path.Combine(_directory, key + ".wav");

    private static bool IsValidWave(byte[] audio)
    {
        try { _ = WaveAudio.GetDuration(audio); return true; }
        catch (InvalidDataException) { return false; }
    }

    /// <summary>A WAV is one byte array; larger sizes come from a corrupt index and could overflow the total.</summary>
    private static bool IsPossibleSize(long size) => size is >= 0 and <= Array.MaxLength;

    private static bool IsValidKey(string key) => key.Length == 64 && key.All(char.IsAsciiHexDigit);

    private static void ValidateKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!IsValidKey(key)) throw new ArgumentException("Invalid speech cache key.", nameof(key));
    }

    private sealed record CacheIndex(long Generation, Dictionary<string, CacheEntry>? Entries);

    /// <summary>Retained WAV size and the last successful playback.</summary>
    private sealed record CacheEntry(long Size, DateTimeOffset LastPlayedAt);
}
