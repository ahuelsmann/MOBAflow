// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using System.Net;
using System.Net.Http;

#if ANDROID
using Xamarin.Android.Net;
#endif

/// <summary>
/// Named HTTP client identifiers and factory helpers for MOBAsmart LAN traffic.
/// </summary>
public static class MobiHttpClientNames
{
    public const string Platform = "MobiPlatform";
    public const string LanHealth = "MobiLanHealth";
    public const string LanDiscovery = "MobiLanDiscovery";
}

/// <summary>
/// Creates dedicated LAN HTTP client instances that bypass system proxy/VPN routing.
/// </summary>
public static class MobiLanHttpClientFactory
{
    public static HttpClient CreateLanProbeClient(
        TimeSpan connectTimeout,
        TimeSpan requestTimeout,
        TimeSpan pooledConnectionLifetime)
    {
        var handler = CreateLanProbeHandler(connectTimeout, pooledConnectionLifetime);
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = requestTimeout,
        };
    }

    public static HttpMessageHandler CreateLanProbeHandler(
        TimeSpan connectTimeout,
        TimeSpan pooledConnectionLifetime,
        int maxConnectionsPerServer = int.MaxValue)
    {
        return new SocketsHttpHandler
        {
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = connectTimeout,
            PooledConnectionLifetime = pooledConnectionLifetime,
            MaxConnectionsPerServer = maxConnectionsPerServer,
        };
    }

    public static HttpMessageHandler CreateLanHealthHandler()
    {
        return CreateLanProbeHandler(
            connectTimeout: TimeSpan.FromSeconds(3),
            pooledConnectionLifetime: TimeSpan.FromMinutes(2));
    }

    public static HttpMessageHandler CreateLanDiscoveryHandler()
    {
        return CreateLanProbeHandler(
            connectTimeout: TimeSpan.FromMilliseconds(600),
            pooledConnectionLifetime: TimeSpan.FromMinutes(2),
            maxConnectionsPerServer: 128);
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
        var handler = CreateLanDiscoveryHandler();
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(2),
        };
    }

    /// <summary>
    /// SignalR transport handler for MOBApi on the LAN. Mirrors REST health probes: no system proxy/VPN routing.
    /// </summary>
    public static HttpMessageHandler CreateLanSignalRHandler()
    {
#if ANDROID
        return new AndroidMessageHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            UseProxy = false,
        };
#else
        return CreateLanHealthHandler();
#endif
    }
}
