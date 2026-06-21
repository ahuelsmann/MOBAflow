// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Tracks MOBAsmart runtime mode: remote MOBAflow session vs direct local Z21 control.
/// </summary>
public interface IMobileRuntimeCoordinator : IRuntimeCommandGateway
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
    /// Updates MOBAflow session availability (connection enabled, REST reachable, hub connected).
    /// </summary>
    void SetMobaflowSessionActive(bool isActive);

    /// <summary>
    /// Updates local Z21 connection state for fallback command routing.
    /// </summary>
    void SetLocalZ21Connected(bool isConnected);
}
