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
}
