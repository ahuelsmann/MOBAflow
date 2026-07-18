// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

/// <summary>
/// Triggers a momentary locomotive function when a feedback input is reported.
/// </summary>
public sealed class LocomotiveWhistleRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Guid LocomotiveId { get; set; }

    /// <summary>One-based feedback input.</summary>
    public int InPort { get; set; }

    /// <summary>Decoder function from F0 through F31.</summary>
    public int FunctionIndex { get; set; } = 2;

    public int DelayMilliseconds { get; set; }

    public int ActiveDurationMilliseconds { get; set; } = 1000;

    public bool Enabled { get; set; } = true;
}
