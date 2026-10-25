// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using Enum;

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

    /// <summary>
    /// Finds the values that prevent this rule from running, in the order of the properties.
    /// </summary>
    /// <returns>The issues; empty when the rule can run.</returns>
    public IReadOnlyList<LocomotiveWhistleRuleIssue> FindIssues()
    {
        var issues = new List<LocomotiveWhistleRuleIssue>();
        if (LocomotiveId == Guid.Empty)
            issues.Add(LocomotiveWhistleRuleIssue.MissingLocomotive);
        if (InPort <= 0)
            issues.Add(LocomotiveWhistleRuleIssue.InvalidInPort);
        if (FunctionIndex is < 0 or > 31)
            issues.Add(LocomotiveWhistleRuleIssue.FunctionOutOfRange);
        if (DelayMilliseconds < 0)
            issues.Add(LocomotiveWhistleRuleIssue.NegativeDelay);
        if (ActiveDurationMilliseconds <= 0)
            issues.Add(LocomotiveWhistleRuleIssue.InvalidActiveDuration);
        return issues;
    }
}
