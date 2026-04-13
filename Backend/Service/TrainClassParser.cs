// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

using Interface;

/// <summary>
/// Implements ITrainClassParser to resolve locomotive class numbers.
/// </summary>
public class TrainClassParser : ITrainClassParser
{
    /// <summary>
    /// Parses a user input and attempts to resolve it to a locomotive series.
    /// Supports flexible input formats: "110", "BR 110", "BR110", "br110", etc.
    /// </summary>
    /// <param name="input">User input (e.g., "110", "BR 110", "br110")</param>
    /// <returns>Resolved LocomotiveSeries if found; otherwise null</returns>
    public LocomotiveSeries? Parse(string input)
    {
        return TrainClassLibrary.TryGetByClassNumber(input);
    }

    /// <summary>
    /// Parses a user input and attempts to resolve it to a locomotive series.
    /// Async variant for potential future extensions (e.g., remote lookup).
    /// </summary>
    /// <param name="input">User input (e.g., "110", "BR 110", "br110")</param>
    /// <returns>Resolved LocomotiveSeries if found; otherwise null</returns>
    public async Task<LocomotiveSeries?> ParseAsync(string input)
    {
        // For now, simply delegate to the synchronous Parse method.
        // In the future, this could be extended to support:
        // - Remote API calls
        // - Database lookups
        // - Async caching
        return await Task.FromResult(Parse(input));
    }
}
