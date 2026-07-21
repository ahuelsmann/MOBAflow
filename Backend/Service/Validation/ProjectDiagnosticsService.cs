// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Validation;

using Domain;

public enum ProjectDiagnosticSeverity
{
    Information,
    Warning,
    Error
}

/// <summary>
/// One current, actionable project finding. Unlike application logs, diagnostics describe
/// the present project state and can therefore be refreshed and de-duplicated by a stable ID.
/// </summary>
public sealed record ProjectDiagnostic(
    string Id,
    ProjectDiagnosticSeverity Severity,
    string Source,
    string Message,
    IReadOnlyList<Guid> TargetIds)
{
    public string SeverityLabel => Severity switch
    {
        ProjectDiagnosticSeverity.Error => "Error",
        ProjectDiagnosticSeverity.Warning => "Warning",
        _ => "Information"
    };
}

public interface IProjectDiagnosticsService
{
    IReadOnlyList<ProjectDiagnostic> Analyze(Project? project);
}

/// <summary>
/// Aggregates platform-neutral project validation into one structured output model.
/// Additional page-specific analyzers can be composed here without coupling the shell to those pages.
/// </summary>
public sealed class ProjectDiagnosticsService(
    IDigitalAddressConflictDetector addressDetector,
    IInterlockingDefinitionValidator interlockingValidator)
    : IProjectDiagnosticsService
{
    private readonly IDigitalAddressConflictDetector _addressDetector =
        addressDetector ?? throw new ArgumentNullException(nameof(addressDetector));
    private readonly IInterlockingDefinitionValidator _interlockingValidator =
        interlockingValidator ?? throw new ArgumentNullException(nameof(interlockingValidator));

    public IReadOnlyList<ProjectDiagnostic> Analyze(Project? project)
    {
        if (project is null)
            return [];

        var diagnostics = new List<ProjectDiagnostic>();
        AddAddressDiagnostics(project, diagnostics);
        AddInterlockingDiagnostics(project, diagnostics);
        AddTrackPlanFeedbackDiagnostics(project, diagnostics);

        return diagnostics
            .OrderBy(diagnostic => SeverityOrder(diagnostic.Severity))
            .ThenBy(diagnostic => diagnostic.Source, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private void AddAddressDiagnostics(Project project, List<ProjectDiagnostic> diagnostics)
    {
        foreach (var finding in _addressDetector.Detect(project).Findings)
        {
            var severity = finding.Kind switch
            {
                DigitalAddressFindingKind.OutOfRange => ProjectDiagnosticSeverity.Error,
                DigitalAddressFindingKind.UnknownMultiplexerMapping => ProjectDiagnosticSeverity.Warning,
                DigitalAddressFindingKind.Conflict when finding.Domain == DigitalAddressDomain.Feedback =>
                    ProjectDiagnosticSeverity.Error,
                DigitalAddressFindingKind.Conflict => ProjectDiagnosticSeverity.Warning,
                _ => ProjectDiagnosticSeverity.Information
            };

            diagnostics.Add(new ProjectDiagnostic(
                $"address:{finding.Id}",
                severity,
                SourceFor(finding.Domain),
                finding.Message,
                finding.Owners.Select(owner => owner.Id).Distinct().Order().ToArray()));
        }
    }

    private void AddInterlockingDiagnostics(
        Project project,
        List<ProjectDiagnostic> diagnostics)
    {
        foreach (var finding in _interlockingValidator.Validate(project).Findings)
        {
            diagnostics.Add(new ProjectDiagnostic(
                $"interlocking:{finding.Id}",
                finding.Severity == InterlockingValidationSeverity.Error
                    ? ProjectDiagnosticSeverity.Error
                    : ProjectDiagnosticSeverity.Warning,
                "Interlocking",
                finding.Message,
                new[] { finding.EntityId }.Concat(finding.RelatedIds).Distinct().Order().ToArray()));
        }
    }

    private static void AddTrackPlanFeedbackDiagnostics(
        Project project,
        List<ProjectDiagnostic> diagnostics)
    {
        var configuredSegments = project.TrackPlan?.Segments
            .Where(segment => segment.InPort.HasValue)
            .ToArray() ?? [];

        foreach (var segment in configuredSegments.Where(segment => segment.InPort <= 0))
        {
            diagnostics.Add(new ProjectDiagnostic(
                $"track-plan:feedback:invalid:{segment.Id:N}",
                ProjectDiagnosticSeverity.Error,
                "Track plan",
                $"Feedback point on track '{segment.Code}' must use a positive InPort.",
                [segment.Id]));
        }

        foreach (var group in configuredSegments
                     .Where(segment => segment.InPort > 0)
                     .GroupBy(segment => segment.InPort!.Value)
                     .Where(group => group.Count() > 1)
                     .OrderBy(group => group.Key))
        {
            var ids = group.Select(segment => segment.Id).Distinct().Order().ToArray();
            diagnostics.Add(new ProjectDiagnostic(
                $"track-plan:feedback:duplicate:{group.Key}:{string.Join(",", ids.Select(id => id.ToString("N")))}",
                ProjectDiagnosticSeverity.Error,
                "Track plan",
                $"InPort {group.Key} is assigned to {ids.Length} feedback points. Each feedback point needs a unique InPort.",
                ids));
        }
    }

    private static string SourceFor(DigitalAddressDomain domain) => domain switch
    {
        DigitalAddressDomain.Locomotive => "Locomotives",
        DigitalAddressDomain.Accessory => "Interlocking",
        DigitalAddressDomain.Feedback => "Interlocking",
        _ => "Project"
    };

    private static int SeverityOrder(ProjectDiagnosticSeverity severity) => severity switch
    {
        ProjectDiagnosticSeverity.Error => 0,
        ProjectDiagnosticSeverity.Warning => 1,
        _ => 2
    };
}
