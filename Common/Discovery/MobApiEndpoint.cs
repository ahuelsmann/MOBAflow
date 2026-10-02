// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Discovery;

/// <summary>
/// Address of a MOBApi instance and the single place that builds its request URIs.
/// MOBApi serves plain HTTP because MOBAflow targets a private household on a trusted home network;
/// see the operating model in SECURITY.md.
/// </summary>
/// <param name="IpAddress">Host name or IP address of the MOBApi instance.</param>
/// <param name="HttpPort">HTTP port of the MOBApi instance.</param>
public sealed record MobApiEndpoint(string IpAddress, int HttpPort)
{
    /// <summary>Returns the endpoint of the MOBApi process on this PC.</summary>
    public static MobApiEndpoint Local(int httpPort) => new("127.0.0.1", httpPort);

    /// <summary>Base URI of the MOBApi instance, for example <c>http://192.168.0.20:5001/</c>.</summary>
    public Uri BaseUri => new UriBuilder(Uri.UriSchemeHttp, IpAddress.Trim(), HttpPort).Uri;

    /// <summary>Resolves a MOBApi path such as <c>api/solution</c> or <c>runtime-hub</c> against <see cref="BaseUri"/>.</summary>
    public Uri Resolve(string relativePath) => new(BaseUri, relativePath);
}
