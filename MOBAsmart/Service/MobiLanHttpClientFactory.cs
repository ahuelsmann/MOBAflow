// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using System.Net;
using System.Net.Http;
using System.Net.Sockets;

/// <summary>
/// Creates dedicated LAN <see cref="HttpClient"/> instances that bypass system proxy/VPN routing.
/// </summary>
internal static class MobiLanHttpClientFactory
{
    public static HttpClient CreateLanProbeClient(
        TimeSpan connectTimeout,
        TimeSpan requestTimeout,
        TimeSpan pooledConnectionLifetime)
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = connectTimeout,
            PooledConnectionLifetime = pooledConnectionLifetime,
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = requestTimeout,
        };
    }

    public static HttpClient CreateLanHealthClient()
    {
        return CreateLanProbeClient(
            connectTimeout: TimeSpan.FromSeconds(3),
            requestTimeout: TimeSpan.FromSeconds(6),
            pooledConnectionLifetime: TimeSpan.FromMinutes(2));
    }

    public static HttpClient CreateLanDiscoveryProbeClient()
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromMilliseconds(600),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 128,
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(2),
        };
    }
}
