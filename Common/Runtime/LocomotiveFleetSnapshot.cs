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

    /// <summary>
    /// Gets custom PNG asset filenames for function buttons F0–F31 (e.g. "headlight.png").
    /// Index 0 = F0, … 31 = F31. Empty entries fall back to defaults on the client.
    /// </summary>
    public IReadOnlyList<string>? FunctionSymbols { get; init; }

    /// <summary>
    /// Gets custom button colors for function buttons F0–F31 (e.g. "#FFD700").
    /// Index 0 = F0, … 31 = F31. Empty entries fall back to defaults on the client.
    /// </summary>
    public IReadOnlyList<string>? FunctionColors { get; init; }

    /// <summary>
    /// Gets manufacturer handbook labels for function buttons F0–F31 (e.g. ESU decoder mapping).
    /// Index 0 = F0, … 31 = F31. Empty entries have no stored label.
    /// </summary>
    public IReadOnlyList<string>? FunctionLabels { get; init; }
}
