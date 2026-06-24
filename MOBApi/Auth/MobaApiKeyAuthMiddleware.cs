// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Auth;

using Common.Security;

using System.Net;

/// <summary>
/// Requires a valid MOBApi pairing key for remote requests.
/// Localhost traffic from MOBAflow remains unauthenticated.
/// </summary>
public sealed class MobaApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public MobaApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (MobaApiAuth.IsPublicPath(path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (MobaApiAuth.IsLocalConnection(
                context.Connection.RemoteIpAddress,
                context.Connection.LocalIpAddress))
        {
            context.Items[MobaApiAuth.AuthenticatedItemKey] = true;
            await _next(context).ConfigureAwait(false);
            return;
        }

        var configuredApiKey = MobaApiAuth.ReadConfiguredApiKey();
        if (string.IsNullOrEmpty(configuredApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response
                .WriteAsJsonAsync(new { error = "MOBApi pairing key is not configured." })
                .ConfigureAwait(false);
            return;
        }

        context.Request.Headers.TryGetValue(MobaApiAuth.ApiKeyHeaderName, out var headerValues);
        var provided = headerValues.FirstOrDefault();
        if (!MobaApiAuth.TryGetProvidedApiKey(provided, out var apiKey)
            || !MobaApiAuth.KeysMatch(apiKey, configuredApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response
                .WriteAsJsonAsync(new { error = "Invalid or missing MOBApi pairing key." })
                .ConfigureAwait(false);
            return;
        }

        context.Items[MobaApiAuth.AuthenticatedItemKey] = true;
        await _next(context).ConfigureAwait(false);
    }
}
