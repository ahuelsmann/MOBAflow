// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

using Domain;

/// <summary>
/// Stable validation codes emitted by Workflow 2.0 validation.
/// </summary>
public static class WorkflowValidationCodes
{
    /// <summary>Two workflows use the same identifier.</summary>
    public const string DuplicateWorkflowId = "WF001";
    /// <summary>An execution request references a workflow outside its project snapshot.</summary>
    public const string MissingWorkflow = "WF002";
    /// <summary>A workflow contains no actions.</summary>
    public const string EmptyWorkflow = "WF100";
    /// <summary>Two actions use the same identifier.</summary>
    public const string DuplicateActionId = "WF102";
    /// <summary>An action identifier is empty.</summary>
    public const string EmptyActionId = "WF103";
    /// <summary>An action contains an invalid or unsupported payload.</summary>
    public const string InvalidActionPayload = "WF107";
}

/// <summary>
/// Identifies the severity of a workflow validation issue.
/// </summary>
public enum WorkflowValidationSeverity
{
    /// <summary>The workflow cannot execute.</summary>
    Error,

    /// <summary>The workflow can execute but should be reviewed.</summary>
    Warning
}

/// <summary>
/// Describes one stable, navigation-ready workflow validation issue.
/// </summary>
/// <param name="Code">Stable validation code.</param>
/// <param name="Severity">Issue severity.</param>
/// <param name="WorkflowId">Affected workflow.</param>
/// <param name="StepId">Affected action, when applicable. Retains the diagnostic field name.</param>
/// <param name="FieldPath">Affected model field.</param>
/// <param name="Message">English diagnostic message.</param>
public sealed record WorkflowValidationIssue(
    string Code,
    WorkflowValidationSeverity Severity,
    Guid WorkflowId,
    Guid? StepId,
    string FieldPath,
    string Message);

/// <summary>
/// Contains all issues produced by one workflow validation pass.
/// </summary>
public sealed class WorkflowValidationResult
{
    private readonly List<WorkflowValidationIssue> _issues = [];

    /// <summary>Gets the issues in deterministic validation order.</summary>
    public IReadOnlyList<WorkflowValidationIssue> Issues => _issues;

    /// <summary>Gets a value indicating whether validation produced no errors.</summary>
    public bool IsValid => _issues.All(issue => issue.Severity != WorkflowValidationSeverity.Error);

    /// <summary>Adds an issue to this result.</summary>
    /// <param name="issue">Issue to append.</param>
    public void Add(WorkflowValidationIssue issue) => _issues.Add(issue);
}

/// <summary>
/// Validates workflow action sequences.
/// </summary>
public interface IWorkflowValidator
{
    /// <summary>Validates every Workflow 2.0 definition in a project.</summary>
    /// <param name="project">Project containing workflows and referenced domain state.</param>
    WorkflowValidationResult Validate(Project project);
}
