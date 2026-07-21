// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Moba.MOBApi.Security;

internal sealed class ProtectedDocumentStore<TDocument> where TDocument : class, new()
{
    private readonly IDataProtector _protector;
    private readonly string _path;

    public ProtectedDocumentStore(IDataProtectionProvider provider, string purpose, string path)
    {
        _protector = provider.CreateProtector(purpose);
        _path = path;
    }

    public async Task<TDocument> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return new TDocument();

        var protectedBytes = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
        var clearBytes = _protector.Unprotect(protectedBytes);
        return JsonSerializer.Deserialize<TDocument>(clearBytes) ??
               throw new InvalidDataException($"Protected document '{Path.GetFileName(_path)}' is empty.");
    }

    public async Task SaveAsync(TDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path) ??
                        throw new InvalidOperationException("Protected document path has no directory.");
        Directory.CreateDirectory(directory);

        var clearBytes = JsonSerializer.SerializeToUtf8Bytes(document);
        var protectedBytes = _protector.Protect(clearBytes);
        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";

        await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken).ConfigureAwait(false);
        File.Move(temporaryPath, _path, true);
        RestrictFilePermissions(_path);
    }

    private static void RestrictFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
            return;

        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}