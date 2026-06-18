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

    /// Persists solution changes when supported by the host (no-op on mobile).

    /// </summary>

    Task SaveSolutionInternalAsync();

}