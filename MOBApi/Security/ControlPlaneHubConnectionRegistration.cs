// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.SignalR;
using Moba.MOBApi.Service;
using System.Globalization;
using System.Security.Claims;

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
    /// Records a runtime-remote presence. During the migration window, this may be a legacy anonymous client.
    /// </summary>
    void RegisterRemote(HubCallerContext context, string credentialId, bool isAnonymousCompatibility);

    /// <summary>
    /// Removes all security and runtime-presence state for a disconnected hub.
    /// </summary>
    void Unregister(HubCallerContext context);
}

internal sealed class ControlPlaneHubConnectionRegistry(
    IRuntimeRemoteRegistry remoteRegistry,
    IControlPlaneConnectionRevoker connectionRevoker,
    ICompatibilityReadConnectionRevoker compatibilityReadConnectionRevoker) : IControlPlaneHubConnectionRegistry
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

    public void RegisterRemote(HubCallerContext context, string credentialId, bool isAnonymousCompatibility)
    {
        remoteRegistry.Register(context.ConnectionId, credentialId);
        if (isAnonymousCompatibility)
            compatibilityReadConnectionRevoker.Register(context.ConnectionId, context.Abort);
    }

    public void Unregister(HubCallerContext context)
    {
        remoteRegistry.Unregister(context.ConnectionId);
        connectionRevoker.Unregister(context.ConnectionId);
        compatibilityReadConnectionRevoker.Unregister(context.ConnectionId);
    }
}
