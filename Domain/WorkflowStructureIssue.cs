// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>A structural problem of a workflow, located by action position and field path.</summary>
/// <param name="Kind">The kind of problem.</param>
/// <param name="ActionIndex">The position of the affected action, or <see langword="null"/> for the workflow itself.</param>
/// <param name="FieldPath">The JSON-style path of the affected field, for example <c>actions[2].id</c>.</param>
public sealed record WorkflowStructureIssue(WorkflowStructureIssueKind Kind, int? ActionIndex, string FieldPath);
