// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Creates the shared workflow library with explicit test collaborators: a dialog that confirms nothing, the
/// production validator, a fresh trace store, a local event bus and a dry-run service that must be replaced
/// by tests that run a workflow.
/// </summary>
internal static class TestWorkflowLibraries
{
    public static WorkflowLibraryViewModel Create(
        IProjectContext projectContext,
        IDialogService? dialogService = null,
        IWorkflowDryRunService? dryRunService = null,
        IWorkflowTraceStore? traceStore = null,
        IEventBus? eventBus = null) =>
        new(
            projectContext,
            dialogService ?? new NullDialogService(),
            new WorkflowValidator(),
            dryRunService ?? new UnavailableWorkflowDryRunService(),
            traceStore ?? new WorkflowTraceStore(),
            eventBus ?? new EventBus(NullLogger<EventBus>.Instance),
            NullLogger<WorkflowLibraryViewModel>.Instance);

    /// <summary>Fails loudly when a test dry-runs a workflow without supplying its own service.</summary>
    private sealed class UnavailableWorkflowDryRunService : IWorkflowDryRunService
    {
        public Task<WorkflowExecutionResult> DryRunAsync(Project project, Workflow workflow, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The test did not provide a workflow dry-run service.");
    }
}
