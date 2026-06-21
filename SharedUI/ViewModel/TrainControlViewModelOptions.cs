// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

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
    /// When true (MOBAsmart), project locomotives are preferred over manual presets on load and in the UI.
    /// </summary>
    public bool PreferProjectLocomotives { get; init; }
}
