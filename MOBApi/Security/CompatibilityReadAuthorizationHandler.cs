// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Claims;

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
    private readonly ICompatibilityReadMigration _readMigration;

    public LiveCapabilityAuthorizationHandler(
        IControlPlaneAccessTokenService accessTokenService,
        ICompatibilityReadMigration readMigration)
    {
        _accessTokenService = accessTokenService;
        _readMigration = readMigration;
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
            if (requirement.AllowAnonymousCompatibility && !credentialPresented)
            {
                var decision = await _readMigration
                    .EvaluateAnonymousReadAsync(ResolveTransport(httpContext), httpContext.RequestAborted)
                    .ConfigureAwait(false);
                if (decision != CompatibilityReadDecision.UpgradeRequired)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    httpContext.Items[CompatibilityReadUpgradeRequired.ItemKey] =
                        new CompatibilityReadUpgradeRequired(ResolveTransport(httpContext));
                }
            }
            return;
        }

        var currentPrincipal = await _accessTokenService
            .ValidateAsync(token, httpContext.RequestAborted)
            .ConfigureAwait(false);
        if (currentPrincipal?.HasClaim(ControlPlaneCapabilities.ClaimType, requirement.Capability) == true)
        {
            context.Succeed(requirement);
            if (string.Equals(requirement.Capability, ControlPlaneCapabilities.Read, StringComparison.Ordinal))
            {
                var credentialId = currentPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(credentialId))
                {
                    await _readMigration
                        .RecordAuthenticatedReadAsync(
                            credentialId,
                            ResolveTransport(httpContext),
                            httpContext.Request.Headers[CompatibilityReadHeaders.ClientRelease].FirstOrDefault(),
                            httpContext.RequestAborted)
                        .ConfigureAwait(false);
                }
            }
        }
    }

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

    private static bool HasPresentedCredential(HttpContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Request.Headers[HeaderNames.Authorization].ToString()))
            return true;

        return (context.Request.Path.StartsWithSegments("/runtime-hub", StringComparison.Ordinal) ||
                context.Request.Path.StartsWithSegments("/photos-hub", StringComparison.Ordinal)) &&
               context.Request.Query.ContainsKey("access_token");
    }

    private static CompatibilityReadTransport ResolveTransport(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/runtime-hub", StringComparison.Ordinal) ||
        context.Request.Path.StartsWithSegments("/photos-hub", StringComparison.Ordinal)
            ? CompatibilityReadTransport.SignalR
            : CompatibilityReadTransport.Rest;
}

internal sealed record CompatibilityReadUpgradeRequired(CompatibilityReadTransport Transport)
{
    public const string ItemKey = "MOBApi.CompatibilityReadUpgradeRequired";
}