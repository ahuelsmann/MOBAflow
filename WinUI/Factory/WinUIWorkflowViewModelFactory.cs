// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Factory;

using Moba.Backend;
using Moba.Backend.Model;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.Sound;

/// <summary>
/// WinUI-specific factory for creating WorkflowViewModel instances with dependencies
/// </summary>
public class WinUIWorkflowViewModelFactory : IWorkflowViewModelFactory
{
    private readonly ISpeakerEngine? _speakerEngine;
    private readonly Project? _project;
    private readonly Z21? _z21;

    public WinUIWorkflowViewModelFactory(
        ISpeakerEngine? speakerEngine = null,
        Project? project = null,
        Z21? z21 = null)
    {
        _speakerEngine = speakerEngine;
        _project = project;
        _z21 = z21;
    }

    public WorkflowViewModel Create(Workflow model)
        => new WorkflowViewModel(model, _speakerEngine, _project, _z21);
}
