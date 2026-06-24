// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

/// <summary>
/// Host-specific options for <see cref="TrainControlViewModel"/>.
/// MOBAsmart registers remote snapshot mode; WinUI uses the default (local runtime snapshots).
/// </summary>
public sealed class TrainControlViewModelOptions
{
    /// <summary>
    /// When true, locomotive state is driven by MOBAflow snapshots via MOBApi.
    /// </summary>
    public bool UseRemoteRuntimeSnapshots { get; init; }

    /// <summary>
    /// When true (MOBAsmart), subscribes to local and remote snapshots and picks the active source dynamically.
    /// </summary>
    public bool HybridRuntimeSnapshots { get; init; }

    /// <summary>
    /// Host that owns locomotive picker selection persistence.
    /// </summary>
    public TrainControlHost Host { get; init; } = TrainControlHost.WinUi;
}
