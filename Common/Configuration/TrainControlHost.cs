// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Identifies which MOBA client owns host-specific train control UI state.
/// </summary>
public enum TrainControlHost
{
    /// <summary>
    /// MOBAflow WinUI desktop client.
    /// </summary>
    WinUi,

    /// <summary>
    /// MOBAsmart MAUI mobile client.
    /// </summary>
    Maui
}
