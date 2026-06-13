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
    /// Gets the active project for runtime execution.
    /// </summary>
    public Project ActiveProject { get; }

    /// <summary>
    /// Gets the journey manager bound to the active project.
    /// </summary>
    public IJourneyManager JourneyManager { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        JourneyManager.Dispose();
    }
}
