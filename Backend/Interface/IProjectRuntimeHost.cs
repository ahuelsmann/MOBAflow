// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Domain;

/// <summary>
/// The runtimes of the projects of the loaded solution, one per project, all running at the same time.
/// The solution session creates and discards them; the selected project only decides which runtime the UI shows.
/// </summary>
public interface IProjectRuntimeHost
{
    /// <summary>Gets the projects whose runtime is connected to its Z21.</summary>
    IReadOnlyCollection<Guid> ConnectedProjectIds { get; }

    /// <summary>
    /// Replaces all runtimes with one runtime per project, in project order. When two projects use the same Z21,
    /// the earlier project connects; a runtime created again for an open Z21 connection takes it over.
    /// Every discarded runtime first sets its known locomotives to speed 0; an empty list discards all runtimes.
    /// </summary>
    Task LoadAsync(IReadOnlyList<Project> projects, CancellationToken cancellationToken = default);

    /// <summary>Creates and starts the runtime of a project added to the solution.</summary>
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>Discards the runtime of a project removed from the solution.</summary>
    Task RemoveAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Recreates the runtime of a project whose Z21 assignment changed.</summary>
    Task ReplaceAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>Refreshes the runtime's snapshot of a project's master data after an editor change.</summary>
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>Shows the runtime of a project in the UI; the other runtimes keep running.</summary>
    void SelectProject(Guid? projectId);
}
