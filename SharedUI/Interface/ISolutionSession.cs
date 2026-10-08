// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using Domain;

using System.ComponentModel;

using ViewModel;

/// <summary>
/// Owns the loaded solution: its file path, dirty and save state, auto-save, and the selected project and journey.
/// Services and ViewModels reach the solution only through this session.
/// </summary>
public interface ISolutionSession : IProjectContext
{
    /// <summary>Raised before the solution is written so subscribers can copy their state into the domain model.</summary>
    event EventHandler? SolutionSaving;

    /// <summary>Raised after a solution was loaded or created so subscribers can read the domain model.</summary>
    event EventHandler? SolutionLoaded;

    /// <summary>Raised before the session replaces the loaded solution, so dependent selections can be cleared.</summary>
    event EventHandler? SolutionReplacing;

    /// <summary>
    /// Raised for a persisted model change observed by auto-save, before the solution is saved.
    /// The sender is the changed view model.
    /// </summary>
    event PropertyChangedEventHandler? ModelChanged;

    /// <summary>Gets the loaded solution (one instance for the application lifetime, replaced in place).</summary>
    Solution Solution { get; }

    /// <summary>Gets or sets the file the solution was loaded from or saved to; <c>null</c> for an unsaved solution.</summary>
    string? CurrentSolutionPath { get; set; }

    /// <summary>Gets whether the solution has changes that were not written yet.</summary>
    bool HasUnsavedChanges { get; }

    /// <summary>Gets whether the solution contains at least one project.</summary>
    bool HasSolution { get; }

    /// <summary>Gets whether the host can write solution files.</summary>
    bool CanSave { get; }

    /// <summary>Gets whether property-driven auto-save is currently suppressed (bulk load or new solution).</summary>
    bool IsAutoSaveSuppressed { get; }

    /// <summary>Writes the solution, asking for a path when none is known and <paramref name="allowPathSelection"/> is set.</summary>
    /// <returns><c>true</c> when the solution was written.</returns>
    Task<bool> SaveAsync(bool allowPathSelection);

    /// <summary>Replaces the solution with a new solution containing one empty project.</summary>
    Task NewSolutionAsync();

    /// <summary>Lets the user pick a solution file and loads it.</summary>
    Task LoadSolutionAsync();

    /// <summary>Loads the solution stored at <paramref name="filePath"/>.</summary>
    Task LoadSolutionFromPathAsync(string filePath);

    /// <summary>Adds a new empty project to the solution and selects it.</summary>
    ProjectViewModel AddProject(Project project);

    /// <summary>Removes <paramref name="project"/> from the solution and selects the first remaining project.</summary>
    void RemoveProject(ProjectViewModel project);

    /// <summary>
    /// Saves the solution whenever <paramref name="source"/> reports a model change. The selected project with its
    /// workflows and trains and the selected journey are tracked automatically. Tracking twice has no extra effect.
    /// </summary>
    void TrackChanges(INotifyPropertyChanged source);

    /// <summary>Stops saving on changes of <paramref name="source"/>.</summary>
    void UntrackChanges(INotifyPropertyChanged source);

    /// <summary>Suppresses property-driven auto-save until the returned scope is disposed.</summary>
    IDisposable SuppressAutoSave();

    /// <summary>Stops further writes; used when the application starts shutting down.</summary>
    void BeginShutdown();

    /// <summary>Waits for a running write to finish and releases the write lock; call once after <see cref="BeginShutdown"/>.</summary>
    Task DrainPendingSaveAsync();
}
