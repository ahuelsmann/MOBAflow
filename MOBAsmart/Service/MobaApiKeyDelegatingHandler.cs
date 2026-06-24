// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Common.Configuration;
using Common.Security;

/// <summary>
/// Adds the MOBApi pairing key to outgoing HTTP requests.
/// </summary>
public sealed class MobaApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly AppSettings _appSettings;

    public MobaApiKeyDelegatingHandler(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        MobaApiAuth.ApplyApiKeyHeader(request.Headers, _appSettings.RestApi.ApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
