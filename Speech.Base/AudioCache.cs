// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

using System.Text.Json;

/// <summary>Serialized, atomic WAV/index storage with a configurable least-recently-played size limit.</summary>
public sealed class AudioCache(string directory, Func<long> sizeLimitBytes, TimeProvider clock) : IAudioCache, IDisposable
{
    private readonly string _directory = Path.GetFullPath(directory);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private bool _initialized;
    private long _generation;

    /// <inheritdoc />
    public long Generation => Interlocked.Read(ref _generation);

    /// <inheritdoc />
    public async Task<byte[]?> TryGetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            await TrimAsync(cancellationToken).ConfigureAwait(false);
            if (!_entries.ContainsKey(key)) return null;
            var path = AudioPath(key);
            var audio = File.Exists(path)
                ? await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)
                : null;
            if (audio is not null && IsValidWave(audio)) return audio;
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            await SaveIndexAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public async Task StoreAsync(string key, ReadOnlyMemory<byte> audio, long generation, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        _ = WaveAudio.GetDuration(audio.Span);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (generation != Generation) return;
            if (audio.Length > Math.Max(0, sizeLimitBytes())) return;
            await WriteAtomicAsync(key + ".wav", audio, cancellationToken).ConfigureAwait(false);
            _entries[key] = new CacheEntry(audio.Length, clock.GetUtcNow());
            await TrimAsync(cancellationToken).ConfigureAwait(false);
            await SaveIndexAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public async Task MarkPlayedAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (_entries.TryGetValue(key, out var entry))
            {
                _entries[key] = entry with { LastPlayedAt = clock.GetUtcNow() };
                await SaveIndexAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public async Task<long> GetSizeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            await TrimAsync(cancellationToken).ConfigureAwait(false);
            return _entries.Values.Sum(entry => entry.Size);
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _generation);
            foreach (var key in _entries.Keys.ToArray())
            {
                await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            }
            await SaveIndexAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        Directory.CreateDirectory(_directory);
        var indexPath = Path.Combine(_directory, "index.json");
        if (File.Exists(indexPath))
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(indexPath, cancellationToken).ConfigureAwait(false);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(bytes);
                _entries = loaded is null ? new(StringComparer.Ordinal) : new(loaded.Where(pair =>
                    IsValidKey(pair.Key) && pair.Value is not null && pair.Value.Size >= 0), StringComparer.Ordinal);
            }
            catch (JsonException) { _entries.Clear(); }
        }
        foreach (var path in Directory.EnumerateFiles(_directory, "*.wav"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = Path.GetFileNameWithoutExtension(path);
            if (IsValidKey(key) && !_entries.ContainsKey(key)) File.Delete(path);
        }
        _initialized = true;
    }

    private async Task TrimAsync(CancellationToken cancellationToken)
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
        var size = _entries.Values.Sum(entry => entry.Size);
        var limit = Math.Max(0, sizeLimitBytes());
        foreach (var entry in _entries.OrderBy(pair => pair.Value.LastPlayedAt).ThenBy(pair => pair.Key, StringComparer.Ordinal).ToArray())
        {
            if (size <= limit) break;
            await RemoveAsync(entry.Key, cancellationToken).ConfigureAwait(false);
            size -= entry.Value.Size;
            changed = true;
        }
        if (changed) await SaveIndexAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(AudioPath(key));
        _entries.Remove(key);
        return Task.CompletedTask;
    }

    private Task SaveIndexAsync(CancellationToken cancellationToken) =>
        WriteAtomicAsync("index.json", JsonSerializer.SerializeToUtf8Bytes(_entries), cancellationToken);

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

    private static bool IsValidKey(string key) => key.Length == 64 && key.All(char.IsAsciiHexDigit);

    private static void ValidateKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!IsValidKey(key)) throw new ArgumentException("Invalid speech cache key.", nameof(key));
    }

    private sealed record CacheEntry(long Size, DateTimeOffset LastPlayedAt);
}
