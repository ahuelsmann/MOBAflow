// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Path;
using Common.Security;

using SharedUI.Interface;

/// <summary>
/// Resolves locomotive and wagon photos against the connected MOBApi REST host.
/// </summary>
public sealed class MauiPhotoUriResolver : IPhotoUriResolver
{
    private readonly IRemoteControlAuthenticatedHttpClient _authenticatedHttpClient;

    public MauiPhotoUriResolver(IRemoteControlAuthenticatedHttpClient authenticatedHttpClient)
    {
        _authenticatedHttpClient = authenticatedHttpClient
            ?? throw new ArgumentNullException(nameof(authenticatedHttpClient));
    }

    /// <inheritdoc />
    public async Task<Stream?> OpenReadAsync(
        string? relativePhotoPath,
        CancellationToken cancellationToken = default)
    {
        var requestPath = RemotePhotoUriBuilder.BuildRelativeApiPath(relativePhotoPath);
        if (requestPath is null)
        {
            return null;
        }

        using var response = await _authenticatedHttpClient
            .GetAsync(requestPath, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content
            .ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return new MemoryStream(content, writable: false);
    }
}