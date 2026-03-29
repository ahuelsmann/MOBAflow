// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Immutable runtime state for a locomotive.
/// </summary>
public sealed class LocomotiveRuntimeSnapshot
{
    /// <summary>
    /// Gets the DCC address of the locomotive.
    /// </summary>
    public int Address { get; init; }

    /// <summary>
    /// Gets the current speed.
    /// </summary>
    public int Speed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the locomotive is driving forward.
    /// </summary>
    public bool IsForward { get; init; }

    /// <summary>
    /// Gets the current function bit mask.
    /// </summary>
    public uint Functions { get; init; }
}
