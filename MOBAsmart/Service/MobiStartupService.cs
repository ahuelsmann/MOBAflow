// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

/// <summary>
/// Explicit startup hook for MOBAsmart singleton services that must be initialized at app launch.
/// </summary>
public sealed class MobiStartupService
{
    private readonly RemoteRuntimeBridge _remoteRuntimeBridge;

    public MobiStartupService(RemoteRuntimeBridge remoteRuntimeBridge)
    {
        _remoteRuntimeBridge = remoteRuntimeBridge;
    }

    /// <summary>
    /// Ensures startup singletons are constructed and subscribed before the first page appears.
    /// </summary>
    public void Initialize()
    {
        _ = _remoteRuntimeBridge;
    }
}
