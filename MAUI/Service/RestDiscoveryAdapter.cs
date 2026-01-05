// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using SharedUI.Interface;

public class RestDiscoveryAdapter : IRestDiscoveryService
{
    private readonly RestApiDiscoveryService _inner;
    public RestDiscoveryAdapter(RestApiDiscoveryService inner) => _inner = inner;
    public Task<(string? ip, int? port)> DiscoverServerAsync() => _inner.GetServerEndpointAsync();
}
