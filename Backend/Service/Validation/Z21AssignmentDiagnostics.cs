// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.Validation;

using Domain;

using ProjectRuntimes;

/// <summary>
/// Reports a Z21 shared by several projects. Every project controls its own Z21; when two
/// projects use the same Z21, the earlier project in the solution connects and the later one stays disconnected.
/// </summary>
public static class Z21AssignmentDiagnostics
{
    private const string Source = "Z21";

    /// <summary>
    /// Analyzes the Z21 assignment of <paramref name="project"/> within its solution.
    /// </summary>
    public static IReadOnlyList<ProjectDiagnostic> Analyze(Project? project, IReadOnlyList<Project> solutionProjects)
    {
        ArgumentNullException.ThrowIfNull(solutionProjects);
        if (project is null)
        {
            return [];
        }

        // A missing Z21 is shown by the runtime status; only a shared Z21 is a project finding.
        var key = Z21ConnectionRegistry.ToKey(project.Z21);
        if (key is null)
        {
            return [];
        }

        var index = IndexOf(solutionProjects, project);
        var sharing = solutionProjects
            .Select((other, otherIndex) => (other, otherIndex))
            .Where(pair => !ReferenceEquals(pair.other, project)
                && string.Equals(Z21ConnectionRegistry.ToKey(pair.other.Z21), key, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (sharing.Count == 0)
        {
            return [];
        }

        var earlier = sharing.FirstOrDefault(pair => pair.otherIndex < index).other;
        if (earlier is not null)
        {
            return
            [
                new ProjectDiagnostic(
                    $"z21:conflict:{key}",
                    ProjectDiagnosticSeverity.Error,
                    Source,
                    $"Z21 {key} is already used by project '{earlier.Name}'. This project does not connect until it has its own Z21.",
                    [project.Id, earlier.Id])
            ];
        }

        var later = sharing.Select(pair => pair.other).ToList();
        return
        [
            new ProjectDiagnostic(
                $"z21:shared:{key}",
                ProjectDiagnosticSeverity.Warning,
                Source,
                $"Z21 {key} is also assigned to {string.Join(", ", later.Select(other => $"'{other.Name}'"))}, which therefore stays disconnected.",
                [project.Id, .. later.Select(other => other.Id)])
        ];
    }

    private static int IndexOf(IReadOnlyList<Project> projects, Project project)
    {
        for (var i = 0; i < projects.Count; i++)
        {
            if (ReferenceEquals(projects[i], project))
            {
                return i;
            }
        }

        return projects.Count;
    }
}
