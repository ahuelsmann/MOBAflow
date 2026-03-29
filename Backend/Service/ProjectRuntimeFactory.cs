// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;

/// <summary>
/// Creates the project instance used by the runtime.
/// </summary>
public sealed class ProjectRuntimeFactory
{
    /// <summary>
    /// Creates the active runtime project.
    /// Phase 1 keeps the live <see cref="Project"/> reference so existing editor workflows continue to work.
    /// The deep-copy runtime model follows in the next refactoring step.
    /// </summary>
    public Project CreateActiveProject(Project editableProject)
    {
        ArgumentNullException.ThrowIfNull(editableProject);
        return editableProject;
    }
}
