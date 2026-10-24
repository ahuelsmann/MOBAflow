// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain.Enum;

/// <summary>Identifies a value of a locomotive whistle rule that cannot be executed.</summary>
public enum LocomotiveWhistleRuleIssue
{
    /// <summary>The rule names no locomotive.</summary>
    MissingLocomotive,

    /// <summary>The feedback input is zero or negative.</summary>
    InvalidInPort,

    /// <summary>The function is outside F0 through F31.</summary>
    FunctionOutOfRange,

    /// <summary>The delay before the function is negative.</summary>
    NegativeDelay,

    /// <summary>The function stays active for zero or a negative time.</summary>
    InvalidActiveDuration
}
