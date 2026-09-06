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
    /// Tracks an authenticated hub connection for credential revocation or an anonymous read connection for migration revocation.
    /// </summary>
    void RegisterReadConnection(HubCallerContext context);

    /// <summary>
    /// Records a runtime-remote presence. During the migration window, this may be a legacy anonymous client.
    /// </summary>
    void RegisterRemote(HubCallerContext context, string credentialId, bool isAnonymousCompatibility);

    /// <summary>
    /// Removes all security and runtime-presence state for a disconnected hub.
    /// </summary>
    void Unregister(HubCallerContext context);

    /// <summary>Revalidates an anonymous hub read after connection registration.</summary>
    Task<CompatibilityReadDecision> EvaluateAnonymousReadAsync(
        CompatibilityReadTransport transport,
        CancellationToken cancellationToken);

    /// <summary>Records a successful authenticated hub read.</summary>
    Task RecordAuthenticatedReadAsync(
        string credentialId,
        CompatibilityReadTransport transport,
        string? clientRelease,
        CancellationToken cancellationToken);
}

internal sealed class ControlPlaneHubConnectionRegistry(
    IRuntimeRemoteRegistry remoteRegistry,
    IControlPlaneConnectionRevoker connectionRevoker,
    ICompatibilityReadConnectionRevoker compatibilityReadConnectionRevoker,
    ICompatibilityReadMigration readMigration) : IControlPlaneHubConnectionRegistry
{
    public void RegisterReadConnection(HubCallerContext context)
    {
        var credentialId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var expiresAtValue = context.User?
            .FindFirst(ControlPlaneCapabilities.AccessTokenExpiresAtClaimType)?
            .Value;
        if (string.IsNullOrWhiteSpace(credentialId) ||
            !long.TryParse(expiresAtValue, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAtUnixSeconds))
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                compatibilityReadConnectionRevoker.Register(context.ConnectionId, context.Abort);
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

    public Task<CompatibilityReadDecision> EvaluateAnonymousReadAsync(
        CompatibilityReadTransport transport,
        CancellationToken cancellationToken) =>
        readMigration.EvaluateAnonymousReadAsync(transport, cancellationToken);

    public Task RecordAuthenticatedReadAsync(
        string credentialId,
        CompatibilityReadTransport transport,
        string? clientRelease,
        CancellationToken cancellationToken) =>
        readMigration.RecordAuthenticatedReadAsync(
            credentialId,
            transport,
            clientRelease,
            cancellationToken);
}
