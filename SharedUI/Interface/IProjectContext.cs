// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using ViewModel;

/// <summary>
/// Project and solution selection context for train control and related views.
/// </summary>
public interface IProjectContext : IJourneySelectionContext
{
    /// <summary>
    /// Gets the solution view model for the active solution.
    /// </summary>
    SolutionViewModel? SolutionViewModel { get; }

    /// <summary>
    /// Gets the outcome of the latest non-interactive persistence attempt.
    /// </summary>
    SolutionSaveState SolutionSaveState { get; }

    /// <summary>
    /// Gets the user-facing status of the latest non-interactive persistence attempt.
    /// </summary>
    string SolutionSaveStatusText { get; }

    /// <summary>
    /// Persists solution changes when supported by the host (no-op on mobile).
    /// </summary>
    Task SaveSolutionInternalAsync();

    /// <summary>
    /// Persists solution changes and returns the resulting host persistence status.
    /// </summary>
    async Task<SolutionSaveResult> SaveSolutionWithStatusAsync()
    {
        await SaveSolutionInternalAsync().ConfigureAwait(false);
        return new SolutionSaveResult(SolutionSaveState, SolutionSaveStatusText);
    }
}

/// <summary>
/// Result of a non-interactive solution persistence attempt.
/// </summary>
public sealed record SolutionSaveResult(SolutionSaveState State, string StatusText);
