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
    /// <summary>A workflow contains no steps.</summary>
    public const string EmptyWorkflow = "WF100";
    /// <summary>The entry step is absent or unresolved.</summary>
    public const string MissingEntryStep = "WF101";
    /// <summary>Two steps use the same identifier.</summary>
    public const string DuplicateStepId = "WF102";
    /// <summary>A step identifier is empty.</summary>
    public const string EmptyStepId = "WF103";
    /// <summary>A step edge cannot be resolved.</summary>
    public const string MissingStepReference = "WF104";
    /// <summary>A step cannot be reached from the entry.</summary>
    public const string UnreachableStep = "WF105";
    /// <summary>The workflow graph contains a cycle.</summary>
    public const string GraphCycle = "WF106";
    /// <summary>A step contains an invalid or unsupported payload.</summary>
    public const string InvalidStepPayload = "WF107";
    /// <summary>An error policy is internally inconsistent.</summary>
    public const string InvalidErrorPolicy = "WF108";
    /// <summary>A retry policy exceeds its bounds.</summary>
    public const string InvalidRetryPolicy = "WF109";
    /// <summary>A nested workflow reference cannot be resolved.</summary>
    public const string MissingNestedWorkflow = "WF110";
    /// <summary>Nested workflow calls are recursive.</summary>
    public const string NestedWorkflowCycle = "WF111";
    /// <summary>A parallel step has no branches.</summary>
    public const string EmptyParallelBranches = "WF112";
    /// <summary>A parallel step repeats a branch entry.</summary>
    public const string DuplicateParallelBranch = "WF113";
    /// <summary>A parallel branch cannot reach its join.</summary>
    public const string InvalidParallelJoin = "WF114";
    /// <summary>Parallel branches overlap before their join.</summary>
    public const string OverlappingParallelBranches = "WF115";
    /// <summary>Parallel branches write the same exclusive resource.</summary>
    public const string ConflictingParallelResource = "WF116";
    /// <summary>A nested workflow chain exceeds the runtime depth bound.</summary>
    public const string NestedWorkflowDepth = "WF117";
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
/// <param name="StepId">Affected step, when applicable.</param>
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
/// Validates Workflow 2.0 graphs and cross-workflow references.
/// </summary>
public interface IWorkflowValidator
{
    /// <summary>Validates every Workflow 2.0 definition in a project.</summary>
    /// <param name="project">Project containing workflows and referenced domain state.</param>
    WorkflowValidationResult Validate(Project project);
}
