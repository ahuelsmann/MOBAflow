// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Interface;
using Domain;

/// <summary>
/// Service for resolving train class input in WinUI context.
/// Provides methods to parse user input and return resolved locomotive series with UI-friendly formatting.
/// </summary>
public class TrainClassResolverService
{
    private readonly ITrainClassParser _parser;

    public TrainClassResolverService(ITrainClassParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    /// <summary>
    /// Resolves train class input and formats it for display.
    /// </summary>
    /// <param name="input">User input (e.g., "110", "BR 110", "br110")</param>
    /// <returns>Formatted display string (e.g., "BR 110.3 (Bügelfalte)") or null if not found</returns>
    public string? ResolveAndFormat(string input)
    {
        var series = _parser.Parse(input);
        return series?.Name;
    }

    /// <summary>
    /// Resolves train class input and returns the full locomotive series.
    /// Useful for binding or getting additional metadata (Vmax, Type, etc).
    /// </summary>
    /// <param name="input">User input (e.g., "110", "BR 110", "br110")</param>
    /// <returns>LocomotiveSeries if found; otherwise null</returns>
    public LocomotiveSeries? Resolve(string input)
    {
        return _parser.Parse(input);
    }

    /// <summary>
    /// Resolves train class input asynchronously.
    /// </summary>
    /// <param name="input">User input (e.g., "110", "BR 110", "br110")</param>
    /// <returns>LocomotiveSeries if found; otherwise null</returns>
    public async Task<LocomotiveSeries?> ResolveAsync(string input)
    {
        return await _parser.ParseAsync(input);
    }

    /// <summary>
    /// Gets all available locomotive classes as a list for auto-completion or combobox binding.
    /// </summary>
    /// <returns>Collection of all available LocomotiveSeries</returns>
    public IReadOnlyCollection<LocomotiveSeries> GetAllAvailableClasses()
    {
        return TrainClassLibrary.GetAllClasses();
    }
}
