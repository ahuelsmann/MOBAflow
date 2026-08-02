// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Moba.MOBApi.Security;

internal sealed record LiveCapabilityRequirement(
    string Capability,
    bool AllowAnonymousCompatibility) : IAuthorizationRequirement;

/// <summary>
/// Live-validates presented credentials for every REST action and SignalR hub invocation.
/// </summary>
internal sealed class LiveCapabilityAuthorizationHandler
    : AuthorizationHandler<LiveCapabilityRequirement>
{
    private readonly IControlPlaneAccessTokenService _accessTokenService;
    private readonly ICompatibilityReadTelemetryRecorder _compatibilityReadTelemetry;
    private readonly ControlPlaneSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    public LiveCapabilityAuthorizationHandler(
        IControlPlaneAccessTokenService accessTokenService,
        ICompatibilityReadTelemetryRecorder compatibilityReadTelemetry,
        IOptions<ControlPlaneSecurityOptions> options,
        TimeProvider timeProvider)
    {
        _accessTokenService = accessTokenService;
        _compatibilityReadTelemetry = compatibilityReadTelemetry;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LiveCapabilityRequirement requirement)
    {
        var httpContext = ResolveHttpContext(context.Resource);
        if (httpContext is null)
        {
            if (context.User.HasClaim(ControlPlaneCapabilities.ClaimType, requirement.Capability))
                context.Succeed(requirement);
            return;
        }

        var credentialPresented = HasPresentedCredential(httpContext);
        var token = GetBearerToken(httpContext);
        if (token is null)
        {
            if (requirement.AllowAnonymousCompatibility &&
                IsAnonymousReadAllowed() &&
                !credentialPresented)
            {
                _compatibilityReadTelemetry.RecordAnonymousRead();
                context.Succeed(requirement);
            }
            return;
        }

        var currentPrincipal = await _accessTokenService
            .ValidateAsync(token, httpContext.RequestAborted)
            .ConfigureAwait(false);
        if (currentPrincipal?.HasClaim(ControlPlaneCapabilities.ClaimType, requirement.Capability) == true)
        {
            if (requirement.Capability == ControlPlaneCapabilities.Read)
                _compatibilityReadTelemetry.RecordAuthenticatedRead();
            context.Succeed(requirement);
        }
    }

    private bool IsAnonymousReadAllowed() =>
        _options.LegacyAnonymousReadsEnabled ||
        _options.AnonymousReadRollbackUntilUtc > _timeProvider.GetUtcNow();

    private static HttpContext? ResolveHttpContext(object? resource) => resource switch
    {
        HttpContext httpContext => httpContext,
        AuthorizationFilterContext filterContext => filterContext.HttpContext,
        HubInvocationContext hubContext => hubContext.Context.GetHttpContext(),
        _ => null
    };

    private static string? GetBearerToken(HttpContext context)
    {
        var authorization = context.Request.Headers[HeaderNames.Authorization].ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var header) &&
            string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return header.Parameter;
        }

        return context.Request.Path.StartsWithSegments("/runtime-hub", StringComparison.Ordinal) ||
               context.Request.Path.StartsWithSegments("/photos-hub", StringComparison.Ordinal)
            ? context.Request.Query["access_token"].FirstOrDefault()
            : null;
    }

    internal static bool HasPresentedCredential(HttpContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Request.Headers[HeaderNames.Authorization].ToString()))
            return true;

        return (context.Request.Path.StartsWithSegments("/runtime-hub", StringComparison.Ordinal) ||
                context.Request.Path.StartsWithSegments("/photos-hub", StringComparison.Ordinal)) &&
               context.Request.Query.ContainsKey("access_token");
    }
}