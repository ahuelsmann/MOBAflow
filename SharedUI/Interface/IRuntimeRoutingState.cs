// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Read-only state of a hybrid runtime routing coordinator, which chooses between a remote MOBAflow session and
/// the handset's direct Z21 connection. MOBAsmart's <see cref="IMobileRuntimeCoordinator"/> provides it. A host
/// without such a coordinator (MOBAflow) registers <see cref="Service.NullRuntimeRoutingState"/>, which answers
/// <see langword="false"/> to every member: callers then rely on their own runtime connection state.
/// </summary>
public interface IRuntimeRoutingState
{
    /// <summary>
    /// Gets whether the coordinator prefers remote MOBAflow snapshots and commands over the direct Z21 connection.
    /// </summary>
    bool PreferRemoteRuntime { get; }

    /// <summary>
    /// Gets whether the coordinator offers a command route (remote session or direct Z21) in addition to the
    /// caller's own runtime connection.
    /// </summary>
    bool CanExecuteCommands { get; }

    /// <summary>
    /// Gets whether the coordinator reports an active direct Z21 UDP connection of the hybrid host.
    /// </summary>
    bool IsLocalZ21Connected { get; }
}
