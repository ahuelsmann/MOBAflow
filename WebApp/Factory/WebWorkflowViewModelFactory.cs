// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Factory;

using Moba.Backend;
using Moba.Backend.Model;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.Sound;

/// <summary>
/// Blazor-specific factory for creating WorkflowViewModel instances with dependencies
/// Note: Dependencies are optional - Blazor typically doesn't need audio/speech in server mode
/// </summary>
public class WebWorkflowViewModelFactory : IWorkflowViewModelFactory
{
    private readonly ISpeakerEngine? _speakerEngine;
    private readonly Project? _project;
    private readonly Z21? _z21;

    // ✅ Constructor with optional DI parameters (allows WebApp to work without Sound/Z21)
    public WebWorkflowViewModelFactory(
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
