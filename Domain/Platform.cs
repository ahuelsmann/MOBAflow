// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Platform (track) at a station — one physical track with optional feedback and workflow.
/// </summary>
public class Platform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Platform"/> class with default identifiers and numbers.
    /// </summary>
    public Platform()
    {
        Id = Guid.NewGuid();
        Number = 1;
        InPort = 0;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the platform.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets an optional display name (e.g. "Gleis 1").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the track / platform number (1-based display, e.g. for announcements).
    /// </summary>
    public uint Number { get; set; }

    /// <summary>
    /// Hardware feedback address (Z21 InPort).
    /// Zero means not configured for feedback on this platform.
    /// </summary>
    public uint InPort { get; set; }

    /// <summary>
    /// Optional workflow executed when feedback is received on this platform's <see cref="InPort"/>.
    /// </summary>
    public Guid? WorkflowId { get; set; }
}