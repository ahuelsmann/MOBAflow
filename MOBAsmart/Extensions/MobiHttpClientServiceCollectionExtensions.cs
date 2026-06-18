// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Extensions;

using Microsoft.Extensions.DependencyInjection;

using Service;

using System.Net;

#if ANDROID
using Xamarin.Android.Net;
#endif

/// <summary>
/// Registers named <see cref="HttpClient"/> instances for MOBAsmart network access.
/// </summary>
public static class MobiHttpClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers platform, LAN health, and LAN discovery HTTP clients via <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static IServiceCollection AddMobiHttpClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient(MobiHttpClientNames.Platform, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .ConfigurePrimaryHttpMessageHandler(CreatePlatformHandler);

        services.AddHttpClient(MobiHttpClientNames.LanHealth, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(6);
        })
        .ConfigurePrimaryHttpMessageHandler(MobiLanHttpClientFactory.CreateLanHealthHandler);

        services.AddHttpClient(MobiHttpClientNames.LanDiscovery, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(2);
        })
        .ConfigurePrimaryHttpMessageHandler(MobiLanHttpClientFactory.CreateLanDiscoveryHandler);

        return services;
    }

    private static HttpMessageHandler CreatePlatformHandler()
    {
#if ANDROID
        return new AndroidMessageHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            // Route RFC1918 MOBApi/health checks directly; system VPN/proxy often breaks LAN (see SocksSocketImpl in logs).
            UseProxy = false,
        };
#else
        return new HttpClientHandler();
#endif
    }
}
