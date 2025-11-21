// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Moba.Backend.Model;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Factory for creating platform-specific WorkflowViewModel instances
/// </summary>
public interface IWorkflowViewModelFactory
{
    /// <summary>
    /// Creates a new WorkflowViewModel for the given Workflow model
    /// </summary>
    /// <param name="model">The Workflow model to wrap</param>
    /// <returns>A platform-specific WorkflowViewModel</returns>
    WorkflowViewModel Create(Workflow model);
}
