// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Represents a feedback point on the track plan.
/// InPort serves as the unique identifier (each value can only be used once per project).
/// </summary>
public class FeedbackPointOnTrack
{
    /// <summary>
    /// The feedback port number. Serves as unique identifier.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Display name for the feedback point (e.g., "Block 1 Entry", "Station A Exit").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of this feedback point's purpose.
    /// </summary>
    public string? Description { get; set; }
}