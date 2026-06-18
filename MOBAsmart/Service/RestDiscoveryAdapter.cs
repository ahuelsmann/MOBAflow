// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using SharedUI.Interface;

/// <summary>
/// Adapts <see cref="RestApiDiscoveryService"/> to <see cref="IRestDiscoveryService"/>.
/// </summary>
public class RestDiscoveryAdapter : IRestDiscoveryService
{
    private readonly RestApiDiscoveryService _inner;
    public RestDiscoveryAdapter(RestApiDiscoveryService inner) => _inner = inner;

    /// <inheritdoc />
    public Task<(string? ip, int? port)> DiscoverServerAsync(string? subnetAnchorIp = null) =>
        _inner.GetServerEndpointByDiscoveryOnlyAsync(subnetAnchorIp);
}