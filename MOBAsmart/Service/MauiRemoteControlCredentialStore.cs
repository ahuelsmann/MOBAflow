// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using Microsoft.Maui.Storage;
using Moba.Common.Security;
using System.Text.Json;

/// <summary>
/// Persists the rotating remote-control credential in the platform-protected MAUI store.
/// </summary>
public sealed class MauiRemoteControlCredentialStore : IRemoteControlCredentialStore
{
    private const string CredentialKey = "mobaflow.remote-control.credential.v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ISecureStorage _secureStorage;

    public MauiRemoteControlCredentialStore(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
    }

    public async Task<RemoteControlCredential?> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var json = await _secureStorage.GetAsync(CredentialKey).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<RemoteControlCredential>(json, JsonOptions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            _secureStorage.Remove(CredentialKey);
            return null;
        }
    }

    public async Task SaveAsync(
        RemoteControlCredential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credential);
        cancellationToken.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(credential, JsonOptions);
        await _secureStorage.SetAsync(CredentialKey, json).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _secureStorage.Remove(CredentialKey);
        return Task.CompletedTask;
    }
}