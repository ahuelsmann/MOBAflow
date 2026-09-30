// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Path;
using Common.Configuration;

using SharedUI.Interface;

/// <summary>
/// Resolves locomotive and wagon photos against the connected MOBApi REST host.
/// </summary>
public sealed class MauiPhotoUriResolver : IPhotoUriResolver
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    public MauiPhotoUriResolver(IHttpClientFactory httpClientFactory, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient(MobiHttpClientNames.Platform);
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<Stream?> OpenReadAsync(
        string? relativePhotoPath,
        CancellationToken cancellationToken = default)
    {
        var requestPath = RemotePhotoUriBuilder.BuildRelativeApiPath(relativePhotoPath);
        var serverIp = _settings.RestApi.CurrentIpAddress?.Trim();
        var serverPort = _settings.RestApi.Port;
        if (requestPath is null || string.IsNullOrEmpty(serverIp) || serverPort <= 0)
        {
            return null;
        }

        using var response = await _httpClient
            .GetAsync(new Uri($"http://{serverIp}:{serverPort}/{requestPath}"), cancellationToken)
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