// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Domain;

using Interface;

/// <summary>
/// Shared runtime dispatch for signal-box aspect changes (MOBAflow desktop and MOBAsmart).
/// </summary>
public static class SignalBoxAspectCommandDispatcher
{
    /// <summary>
    /// Sends a signal aspect change to the active runtime command gateway.
    /// </summary>
    public static async Task DispatchAsync(
        IRuntimeCommandGateway gateway,
        Guid signalId,
        SignalAspect aspect,
        IDictionary<Guid, SignalAspect> pendingAspects,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(pendingAspects);

        pendingAspects[signalId] = aspect;

        try
        {
            await gateway.SetSignalAspectAsync(signalId, aspect, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            pendingAspects.Remove(signalId);
            throw;
        }
    }
}
