// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Moba.MOBApi.Security;

/// <summary>
/// Provides constants for the control-plane bearer authentication scheme.
/// </summary>
public static class ControlPlaneAuthenticationDefaults
{
    public const string Scheme = "ControlPlaneBearer";
}

internal sealed class ControlPlaneAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IControlPlaneAccessTokenService _accessTokenService;

    public ControlPlaneAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IControlPlaneAccessTokenService accessTokenService)
        : base(options, logger, encoder)
    {
        _accessTokenService = accessTokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = GetBearerToken();
        if (token is null)
            return AuthenticateResult.NoResult();

        var principal = await _accessTokenService.ValidateAsync(token, Context.RequestAborted).ConfigureAwait(false);
        if (principal is null)
            return AuthenticateResult.Fail("The control-plane bearer token is invalid or expired.");

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Task.CompletedTask;
    }

    private string? GetBearerToken()
    {
        var authorization = Request.Headers[HeaderNames.Authorization].ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var header) &&
            string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            return header.Parameter;

        return IsHubRequest() ? Request.Query["access_token"].FirstOrDefault() : null;
    }

    private bool IsHubRequest() =>
        Request.Path.StartsWithSegments("/runtime-hub", StringComparison.Ordinal) ||
        Request.Path.StartsWithSegments("/photos-hub", StringComparison.Ordinal);
}