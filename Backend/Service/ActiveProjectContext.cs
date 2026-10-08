// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

using Manager;

/// <summary>
/// Holds the currently active project and its runtime services.
/// </summary>
public sealed class ActiveProjectContext : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveProjectContext"/> class.
    /// </summary>
    public ActiveProjectContext(Project activeProject, IJourneyManager journeyManager)
    {
        ArgumentNullException.ThrowIfNull(activeProject);
        ArgumentNullException.ThrowIfNull(journeyManager);

        ActiveProject = activeProject;
        JourneyManager = journeyManager;
    }

    /// <summary>
    /// Gets the runtime's snapshot of the active project's master data.
    /// </summary>
    public Project ActiveProject { get; private set; }

    /// <summary>
    /// Gets the journey manager bound to the active project.
    /// </summary>
    public IJourneyManager JourneyManager { get; }

    /// <summary>
    /// Replaces the master-data snapshot after an editor change; the journey manager keeps its runtime state.
    /// </summary>
    public void ReplaceDefinitions(Project definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        JourneyManager.UpdateDefinitions(definitions);
        ActiveProject = definitions;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        JourneyManager.Dispose();
    }
}