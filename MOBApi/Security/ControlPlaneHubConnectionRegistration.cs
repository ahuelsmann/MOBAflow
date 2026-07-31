// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Moba.MOBApi.Security;

internal static class ControlPlaneHubConnectionRegistration
{
    public static void Register(
        HubCallerContext context,
        IControlPlaneConnectionRevoker connectionRevoker)
    {
        var credentialId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var expiresAtValue = context.User?
            .FindFirst(ControlPlaneCapabilities.AccessTokenExpiresAtClaimType)?
            .Value;
        if (string.IsNullOrWhiteSpace(credentialId) ||
            !long.TryParse(expiresAtValue, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAtUnixSeconds))
        {
            return;
        }

        connectionRevoker.Register(
            context.ConnectionId,
            credentialId,
            DateTimeOffset.FromUnixTimeSeconds(expiresAtUnixSeconds),
            context.Abort);
    }
}
