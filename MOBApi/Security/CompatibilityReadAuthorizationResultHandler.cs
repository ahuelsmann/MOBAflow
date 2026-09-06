// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Moba.MOBApi.Security;

/// <summary>
/// Returns one transport-neutral upgrade reason when anonymous compatibility reads are disabled.
/// </summary>
internal sealed class CompatibilityReadAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded &&
            context.Items.TryGetValue(CompatibilityReadUpgradeRequired.ItemKey, out var value) &&
            value is CompatibilityReadUpgradeRequired upgradeRequired)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return context.Response.WriteAsJsonAsync(
                new
                {
                    type = "https://mobaflow.app/problems/client-upgrade-required",
                    title = "Client upgrade required",
                    status = StatusCodes.Status401Unauthorized,
                    code = "client_upgrade_required",
                    transport = CompatibilityReadTransportNames.GetProtocolName(upgradeRequired.Transport)
                },
                options: null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }

        return _fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}
