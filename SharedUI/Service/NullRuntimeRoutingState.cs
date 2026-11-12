// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Interface;

/// <summary>
/// Routing state of a host without remote runtime routing (MOBAflow desktop): it never prefers a remote runtime
/// and adds no command route beyond the host's own runtime connection.
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
