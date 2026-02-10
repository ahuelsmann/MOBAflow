// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Moba.Domain;

/// <summary>
/// Parses user input for locomotive class numbers and resolves them to full LocomotiveSeries.
/// Supports flexible input formats: "110", "BR 110", "BR110", "br110", etc.
/// </summary>
public interface ITrainClassParser
{
    /// <summary>
    /// Parses a user input and attempts to resolve it to a locomotive series.
    /// </summary>
    /// <param name="input">User input (e.g., "110", "BR 110", "br110")</param>
    /// <returns>Resolved LocomotiveSeries if found; otherwise null</returns>
    LocomotiveSeries? Parse(string input);

    /// <summary>
    /// Parses a user input and attempts to resolve it to a locomotive series.
    /// Async variant for potential future extensions (e.g., remote lookup).
    /// </summary>
    /// <param name="input">User input (e.g., "110", "BR 110", "br110")</param>
    /// <returns>Resolved LocomotiveSeries if found; otherwise null</returns>
    Task<LocomotiveSeries?> ParseAsync(string input);
}
