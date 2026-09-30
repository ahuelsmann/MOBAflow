// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Configuration;

/// <summary>
/// Sends requests from MOBAflow to the MOBApi process on this PC over plain HTTP.
/// Relative request URIs are resolved against the configured REST API port.
/// </summary>
public sealed partial class LocalMobApiClient(AppSettings appSettings) : IDisposable
{
    private const int DefaultPort = 5001;
    private readonly HttpClient _httpClient = new();

    /// <summary>Base address of the local MOBApi process, for example <c>http://127.0.0.1:5001/</c>.</summary>
    public Uri BaseUri => new($"http://127.0.0.1:{Port}/");

    private int Port => appSettings.RestApi.Port > 0 ? appSettings.RestApi.Port : DefaultPort;

    /// <summary>
    /// Sends <paramref name="request"/>; a relative request URI is resolved against <see cref="BaseUri"/>.
    /// </summary>
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestUri is { IsAbsoluteUri: false } relativeUri)
        {
            request.RequestUri = new Uri(BaseUri, relativeUri);
        }

        return _httpClient.SendAsync(request, cancellationToken);
    }

    public void Dispose() => _httpClient.Dispose();
}
