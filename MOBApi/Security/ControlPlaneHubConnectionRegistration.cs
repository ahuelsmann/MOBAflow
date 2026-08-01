// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Moba.MOBApi.Service;

namespace Moba.MOBApi.Security;

/// <summary>
/// Owns the security and runtime-presence lifecycle of SignalR connections.
/// </summary>
public interface IControlPlaneHubConnectionRegistry
{
    /// <summary>
    /// Tracks an authenticated hub connection for immediate credential revocation.
    /// </summary>
    void RegisterAuthenticated(HubCallerContext context);

    /// <summary>
    /// Records an authenticated runtime-remote presence.
    /// </summary>
    void RegisterRemote(HubCallerContext context, string credentialId);

    /// <summary>
    /// Removes all security and runtime-presence state for a disconnected hub.
    /// </summary>
    void Unregister(HubCallerContext context);
}

internal sealed class ControlPlaneHubConnectionRegistry(
    IRuntimeRemoteRegistry remoteRegistry,
    IControlPlaneConnectionRevoker connectionRevoker) : IControlPlaneHubConnectionRegistry
{
    public void RegisterAuthenticated(HubCallerContext context)
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

    public void RegisterRemote(HubCallerContext context, string credentialId) =>
        remoteRegistry.Register(context.ConnectionId, credentialId);

    public void Unregister(HubCallerContext context)
    {
        remoteRegistry.Unregister(context.ConnectionId);
        connectionRevoker.Unregister(context.ConnectionId);
    }
}
