// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Service;

/// <summary>
/// Resolves the HTTP port MOBApi listens on, so Kestrel, the status endpoint and LAN discovery agree.
/// </summary>
internal static class MobApiHttpPort
{
    /// <summary>Port used when neither <c>MOBAFLOW_HTTP_PORT</c> nor an HTTP URL in <c>urls</c> is configured.</summary>
    public const int Default = 5001;

    /// <summary>
    /// Returns <c>MOBAFLOW_HTTP_PORT</c> when valid, otherwise the port of the first HTTP URL in <c>urls</c>,
    /// otherwise <see cref="Default"/>.
    /// </summary>
    public static int Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredPort = configuration.GetValue<int?>("MOBAFLOW_HTTP_PORT");
        if (configuredPort is > 0 and < 65536)
            return configuredPort.Value;

        var urls = configuration["urls"];
        if (!string.IsNullOrWhiteSpace(urls))
        {
            foreach (var value in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttp)
                    return uri.Port;
            }
        }

        return Default;
    }
}
