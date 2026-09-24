// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;

/// <summary>Retains an invalid action without initializing payloads or preventing project editing.</summary>
public sealed class InvalidWorkflowActionViewModel(WorkflowAction action) : WorkflowActionViewModel(action, action.Type)
{
    /// <summary>Gets the recovery instruction displayed instead of a typed payload editor.</summary>
    public string ValidationHint => "This action is invalid or unsupported. Remove it and add a new action with the required settings.";
}
