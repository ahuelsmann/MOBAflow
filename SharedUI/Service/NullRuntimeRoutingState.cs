// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

/// <summary>
/// Routing state of a host without a hybrid routing coordinator (MOBAflow desktop). It answers
/// <see langword="false"/> to every member, also while the host is connected: there is no coordinator, so callers
/// use their own runtime connection state.
/// </summary>
public sealed class NullRuntimeRoutingState : IRuntimeRoutingState
{
    /// <inheritdoc />
    public bool PreferRemoteRuntime => false;

    /// <inheritdoc />
    public bool CanExecuteCommands => false;

    /// <inheritdoc />
    public bool IsLocalZ21Connected => false;
}
