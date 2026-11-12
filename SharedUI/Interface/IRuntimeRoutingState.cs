// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Read-only runtime routing state of a host: whether commands and snapshots come from a remote MOBAflow session
/// or the local Z21. MOBAsmart's <see cref="IMobileRuntimeCoordinator"/> provides it; MOBAflow has no remote
/// routing and registers <see cref="Service.NullRuntimeRoutingState"/>.
/// </summary>
public interface IRuntimeRoutingState
{
    /// <summary>
    /// Gets whether remote MOBAflow snapshots and commands should be preferred.
    /// </summary>
    bool PreferRemoteRuntime { get; }

    /// <summary>
    /// Gets whether any command gateway (remote or local) can execute control commands.
    /// </summary>
    bool CanExecuteCommands { get; }

    /// <summary>
    /// Gets whether the handset has an active direct Z21 UDP connection.
    /// </summary>
    bool IsLocalZ21Connected { get; }
}
