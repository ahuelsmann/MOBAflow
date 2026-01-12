// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Graph;

/// <summary>
/// Represents an isolator (rail gap/insulator) at a specific track port.
/// Isolators electrically separate track sections for block detection.
/// </summary>
public sealed class Isolator
{
    public required Guid Id { get; init; }

    /// <summary>
    /// The track edge where the isolator is located.
    /// </summary>
    public required Guid EdgeId { get; init; }

    /// <summary>
    /// The port ID on the edge where the isolator is placed (e.g., "A", "B").
    /// </summary>
    public required string PortId { get; init; }
}
