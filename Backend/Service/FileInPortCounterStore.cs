// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Interface;
using System.Text.Json;

/// <summary>Stores local counts as an atomically replaced JSON file in application data.</summary>
public sealed class FileInPortCounterStore(string path) : IInPortCounterStore
{
    private readonly string _path = Path.GetFullPath(path);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<uint, ulong>> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("The counter file must contain an object of input numbers and counts.");

            var counts = new Dictionary<uint, ulong>();
            foreach (var entry in document.RootElement.EnumerateObject())
            {
                if (!uint.TryParse(entry.Name, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out var inPort)
                    || inPort == 0 || entry.Value.ValueKind != JsonValueKind.Number
                    || !entry.Value.TryGetUInt64(out var count) || !counts.TryAdd(inPort, count))
                    throw new InvalidDataException("The counter file contains an invalid or duplicate input count.");
            }

            return counts;
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<uint, ulong>();
        }
        catch (DirectoryNotFoundException)
        {
            return new Dictionary<uint, ulong>();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(IReadOnlyDictionary<uint, ulong> counts, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, counts, cancellationToken: cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
