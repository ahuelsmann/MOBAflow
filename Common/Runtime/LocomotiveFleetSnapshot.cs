// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Immutable runtime projection of one project locomotive for mobile fleet selection.
/// </summary>
public sealed record LocomotiveFleetSnapshot
{
    /// <summary>Gets the domain locomotive id.</summary>
    public Guid LocomotiveId { get; init; }

    /// <summary>Gets the user-facing locomotive name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the optional DCC address.</summary>
    public uint? DigitalAddress { get; init; }

    /// <summary>Gets the optional relative photo path from the project.</summary>
    public string? PhotoPath { get; init; }
}
