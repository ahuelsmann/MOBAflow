// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Backend.Interface;
using Backend.Service;

using Common.Extension;
using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;

using WorkflowSteps;

/// <summary>Describes one project reference that prevents silent workflow deletion.</summary>
public sealed record WorkflowReference(string OwnerType, Guid OwnerId, string OwnerName, string Location);

/// <summary>Groups optional services used by workflow dry runs and retained lifecycle traces.</summary>
public sealed class WorkflowLibraryRuntimeServices
{
    /// <summary>Gets the optional workflow execution service.</summary>
    public IWorkflowService? WorkflowService { get; init; }

    /// <summary>Gets the optional retained lifecycle trace store.</summary>
    public IWorkflowTraceStore? TraceStore { get; init; }

    /// <summary>Gets the optional base action execution context.</summary>
    public ActionExecutionContext? ExecutionContext { get; init; }

    /// <summary>Gets the optional lifecycle event bus.</summary>
    public IEventBus? EventBus { get; init; }
}

/// <summary>
/// Owns the shared workflow catalog, editor selection, validation, reference safety, and save coordination.
/// </summary>
public sealed partial class WorkflowLibraryViewModel : ObservableObject, IDisposable
{
    private readonly IProjectContext _projectContext;
    private readonly IDialogService _dialogService;
    private readonly IWorkflowValidator _validator;
    private readonly ILogger<WorkflowLibraryViewModel>? _logger;
    private readonly IWorkflowService? _workflowService;
    private readonly IWorkflowTraceStore? _traceStore;
    private readonly ActionExecutionContext? _executionContext;
    private readonly IEventBus? _eventBus;
    private readonly Guid? _traceSubscriptionId;
    private CancellationTokenSource? _dryRunCancellation;
    private ProjectViewModel? _subscribedProject;
    private bool _suppressAutoSave;
    private bool _disposed;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private WorkflowViewModel? _selectedWorkflow;

    [ObservableProperty]
    private WorkflowStepViewModel? _selectedStep;

    [ObservableProperty]
    private string _lastDeletionBlockMessage = string.Empty;

    [ObservableProperty]
    private bool _isDryRunRunning;

    [ObservableProperty]
    private string _lastDryRunStatus = "Not run";

    /// <summary>Creates a shared workflow-library coordinator.</summary>
    public WorkflowLibraryViewModel(
        IProjectContext projectContext,
        IDialogService? dialogService = null,
        IWorkflowValidator? validator = null,
        ILogger<WorkflowLibraryViewModel>? logger = null,
        WorkflowLibraryRuntimeServices? runtimeServices = null)
    {
        ArgumentNullException.ThrowIfNull(projectContext);
        runtimeServices ??= new WorkflowLibraryRuntimeServices();
        _projectContext = projectContext;
        _dialogService = dialogService ?? new NullDialogService();
        _validator = validator ?? new WorkflowValidator();
        _logger = logger;
        _workflowService = runtimeServices.WorkflowService;
        _traceStore = runtimeServices.TraceStore;
        _executionContext = runtimeServices.ExecutionContext;
        _eventBus = runtimeServices.EventBus;
        if (_eventBus != null)
        {
            _traceSubscriptionId = _eventBus.Subscribe<WorkflowLifecycleEvent>(OnWorkflowLifecycleEvent);
        }
        _projectContext.PropertyChanged += OnProjectContextPropertyChanged;
        AttachProject(_projectContext.SelectedProject);
        RefreshTrace();
    }

    /// <summary>Gets the authoritative wrappers owned by the selected project.</summary>
    public ObservableCollection<WorkflowViewModel>? Workflows => _projectContext.SelectedProject?.Workflows;

    /// <summary>Gets workflows filtered without creating replacement wrappers.</summary>
    public IEnumerable<WorkflowViewModel> FilteredWorkflows => (Workflows ?? [])
        .Where(workflow => string.IsNullOrWhiteSpace(SearchText)
            || workflow.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets validation issues for the selected project in deterministic validator order.</summary>
    public ObservableCollection<WorkflowValidationIssue> ValidationIssues { get; } = [];

    /// <summary>Gets references that prevented the last deletion attempt.</summary>
    public ObservableCollection<WorkflowReference> DeletionReferences { get; } = [];

    /// <summary>Gets the selected workflow's retained lifecycle entries in newest-first order.</summary>
    public ObservableCollection<WorkflowLifecycleEvent> TraceEntries { get; } = [];

    /// <summary>Gets the last dry run's planned external effects in deterministic order.</summary>
    public ObservableCollection<WorkflowPlannedEffect> PlannedEffects { get; } = [];

    /// <summary>Gets whether a dry run is available in the current host.</summary>
    public bool CanDryRun => _workflowService != null && _executionContext != null && SelectedWorkflow != null;

    /// <summary>Gets the node editor target, or the workflow when no node is selected.</summary>
    public object? SelectedEditorObject => (object?)SelectedStep ?? SelectedWorkflow;

    partial void OnSearchTextChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FilteredWorkflows));
    }

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? oldValue, WorkflowViewModel? newValue)
    {
        _ = oldValue;
        SelectedStep = newValue?.Steps.FirstOrDefault();
        Validate();
        RefreshTrace();
        OnPropertyChanged(nameof(CanDryRun));
        DryRunSelectedWorkflowCommand.NotifyCanExecuteChanged();
        CancelDryRunCommand.NotifyCanExecuteChanged();
        DuplicateSelectedWorkflowCommand.NotifyCanExecuteChanged();
        DeleteSelectedWorkflowCommand.NotifyCanExecuteChanged();
        AssignSelectedWorkflowCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedEditorObject));
    }

    /// <summary>Creates a valid minimal workflow and selects its authoritative wrapper.</summary>
    [RelayCommand]
    private async Task CreateWorkflowAsync()
    {
        var project = _projectContext.SelectedProject;
        if (project == null)
        {
            return;
        }

        var terminate = new WorkflowTerminateStep
        {
            Name = "Complete workflow",
            Result = WorkflowTerminationResult.Succeeded
        };
        var workflow = new Workflow
        {
            Name = CreateUniqueName(project, "New Workflow"),
            EntryStepId = terminate.Id,
            Steps = [terminate]
        };
        _suppressAutoSave = true;
        try
        {
            SelectedWorkflow = project.AddWorkflow(workflow);
            AttachWorkflow(SelectedWorkflow);
            NotifyCatalogChanged();
        }
        finally
        {
            _suppressAutoSave = false;
        }

        await SaveAsync();
    }

    /// <summary>Selects one authoritative project workflow wrapper.</summary>
    [RelayCommand]
    private void SelectWorkflow(WorkflowViewModel? workflow)
    {
        if (workflow == null || Workflows?.Contains(workflow) is true)
        {
            SelectedWorkflow = workflow;
        }
    }

    /// <summary>Selects one typed node from the current graph.</summary>
    [RelayCommand]
    private void SelectStep(WorkflowStepViewModel? step)
    {
        if (step == null || (SelectedWorkflow != null && SelectedWorkflow.Steps.Contains(step)))
        {
            SelectedStep = step;
        }
    }

    /// <summary>Duplicates the selected graph with new workflow, node, and action identifiers.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedWorkflow))]
    private async Task DuplicateSelectedWorkflowAsync()
    {
        var project = _projectContext.SelectedProject;
        if (project == null || SelectedWorkflow == null)
        {
            return;
        }

        var duplicate = Duplicate(SelectedWorkflow.Model);
        duplicate.Name = CreateUniqueName(project, $"{SelectedWorkflow.Name} Copy");
        _suppressAutoSave = true;
        try
        {
            SelectedWorkflow = project.AddWorkflow(duplicate);
            AttachWorkflow(SelectedWorkflow);
            NotifyCatalogChanged();
        }
        finally
        {
            _suppressAutoSave = false;
        }

        await SaveAsync();
    }

    /// <summary>Deletes an unreferenced workflow after confirmation; referenced workflows remain intact.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedWorkflow))]
    private async Task DeleteSelectedWorkflowAsync()
    {
        var project = _projectContext.SelectedProject;
        var selected = SelectedWorkflow;
        if (project == null || selected == null)
        {
            return;
        }

        var references = FindReferences(project.Model, selected.Id);
        DeletionReferences.Clear();
        foreach (var reference in references)
        {
            DeletionReferences.Add(reference);
        }

        if (references.Count > 0)
        {
            LastDeletionBlockMessage = BuildReferenceMessage(selected.Name, references);
            await _dialogService.ShowConfirmationAsync(
                "Workflow is in use",
                LastDeletionBlockMessage,
                "OK",
                "Cancel",
                false);
            return;
        }

        LastDeletionBlockMessage = string.Empty;
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete workflow",
            $"Delete workflow '{selected.Name}'?",
            "Delete",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        var nextIndex = project.Workflows.IndexOf(selected);
        DetachWorkflow(selected);
        _suppressAutoSave = true;
        try
        {
            project.RemoveWorkflow(selected);
            SelectedWorkflow = project.Workflows.Count == 0
                ? null
                : project.Workflows[Math.Min(nextIndex, project.Workflows.Count - 1)];
            NotifyCatalogChanged();
        }
        finally
        {
            _suppressAutoSave = false;
        }

        await SaveAsync();
    }

    /// <summary>Assigns the selected workflow to one journey feedback occurrence.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedWorkflow))]
    private async Task AssignSelectedWorkflowAsync(JourneyFeedbackStepViewModel? feedbackStep)
    {
        if (SelectedWorkflow == null || feedbackStep == null)
        {
            return;
        }

        feedbackStep.WorkflowId = SelectedWorkflow.Id;
        await SaveAsync();
    }

    /// <summary>Plans the selected workflow without invoking live action handlers or waiting.</summary>
    [RelayCommand(CanExecute = nameof(CanStartDryRun))]
    private async Task DryRunSelectedWorkflowAsync()
    {
        var project = _projectContext.SelectedProject?.Model;
        var workflow = SelectedWorkflow?.Model;
        if (project == null || workflow == null || _workflowService == null || _executionContext == null)
        {
            return;
        }

        _dryRunCancellation?.Dispose();
        _dryRunCancellation = new CancellationTokenSource();
        IsDryRunRunning = true;
        LastDryRunStatus = "Planning...";
        PlannedEffects.Clear();
        DryRunSelectedWorkflowCommand.NotifyCanExecuteChanged();
        CancelDryRunCommand.NotifyCanExecuteChanged();
        try
        {
            var context = new ActionExecutionContextFactory(_executionContext).Create(new ActionExecutionContextState
            {
                CurrentProject = project
            });
            var result = await _workflowService.ExecuteAsync(new WorkflowExecutionRequest
            {
                Project = project,
                Workflow = workflow,
                Context = context,
                Mode = WorkflowRunMode.DryRun,
                SourceCorrelationId = Guid.NewGuid()
            }, _dryRunCancellation.Token);

            foreach (var effect in result.PlannedEffects)
            {
                PlannedEffects.Add(effect);
            }

            LastDryRunStatus = result.Status.ToString();
        }
        catch (OperationCanceledException)
        {
            LastDryRunStatus = "Cancelled";
        }
        finally
        {
            IsDryRunRunning = false;
            RefreshTrace();
            DryRunSelectedWorkflowCommand.NotifyCanExecuteChanged();
            CancelDryRunCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedStepChanged(WorkflowStepViewModel? value)
    {
        _ = value;
        OnPropertyChanged(nameof(SelectedEditorObject));
    }

    /// <summary>Cancels the active dry run.</summary>
    [RelayCommand(CanExecute = nameof(IsDryRunRunning))]
    private void CancelDryRun()
    {
        _dryRunCancellation?.Cancel();
    }

    /// <summary>Refreshes the bounded read-only trace projection for the selected workflow.</summary>
    [RelayCommand]
    private void RefreshTrace()
    {
        TraceEntries.Clear();
        if (_traceStore == null)
        {
            return;
        }

        var workflowId = SelectedWorkflow?.Id;
        foreach (var entry in _traceStore.GetEntries()
                     .Where(entry => !workflowId.HasValue || entry.WorkflowId == workflowId.Value)
                     .TakeLast(200)
                     .Reverse())
        {
            TraceEntries.Add(entry);
        }
    }

    /// <summary>Refreshes project validation and returns the selected workflow's issues.</summary>
    [RelayCommand]
    private void Validate()
    {
        ValidationIssues.Clear();
        var project = _projectContext.SelectedProject?.Model;
        if (project == null)
        {
            return;
        }

        foreach (var issue in _validator.Validate(project).Issues)
        {
            if (SelectedWorkflow == null || issue.WorkflowId == SelectedWorkflow.Id)
            {
                ValidationIssues.Add(issue);
            }
        }
    }

    /// <summary>Selects the workflow and step identified by one validation issue.</summary>
    [RelayCommand]
    private void NavigateToValidationIssue(WorkflowValidationIssue? issue)
    {
        if (issue == null)
        {
            return;
        }

        var workflow = Workflows?.FirstOrDefault(candidate => candidate.Id == issue.WorkflowId);
        if (workflow == null)
        {
            return;
        }

        SelectedWorkflow = workflow;
        SelectedStep = issue.StepId.HasValue
            ? workflow.Steps.FirstOrDefault(step => step.Id == issue.StepId.Value)
            : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dryRunCancellation?.Cancel();
        _dryRunCancellation?.Dispose();
        if (_eventBus != null && _traceSubscriptionId.HasValue)
        {
            _eventBus.Unsubscribe(_traceSubscriptionId.Value);
        }
        _projectContext.PropertyChanged -= OnProjectContextPropertyChanged;
        AttachProject(null);
        GC.SuppressFinalize(this);
    }

    private bool HasSelectedWorkflow() => SelectedWorkflow != null;

    private bool CanStartDryRun() => CanDryRun && !IsDryRunRunning;

    private void AttachProject(ProjectViewModel? project)
    {
        if (_subscribedProject != null)
        {
            _subscribedProject.Workflows.CollectionChanged -= OnWorkflowsCollectionChanged;
            foreach (var workflow in _subscribedProject.Workflows)
            {
                DetachWorkflow(workflow);
            }
        }

        _subscribedProject = project;
        if (project != null)
        {
            project.Workflows.CollectionChanged += OnWorkflowsCollectionChanged;
            foreach (var workflow in project.Workflows)
            {
                AttachWorkflow(workflow);
            }
        }

        SelectedWorkflow = project?.Workflows.FirstOrDefault();
        NotifyCatalogChanged();
    }

    private void AttachWorkflow(WorkflowViewModel? workflow)
    {
        if (workflow != null)
        {
            workflow.PropertyChanged -= OnWorkflowPropertyChanged;
            workflow.PropertyChanged += OnWorkflowPropertyChanged;
        }
    }

    private void DetachWorkflow(WorkflowViewModel workflow)
    {
        workflow.PropertyChanged -= OnWorkflowPropertyChanged;
    }

    private void OnProjectContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (e.PropertyName == nameof(IProjectContext.SelectedProject))
        {
            AttachProject(_projectContext.SelectedProject);
        }
    }

    private void OnWorkflowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var workflow in e.OldItems.OfType<WorkflowViewModel>())
            {
                DetachWorkflow(workflow);
            }
        }

        if (e.NewItems != null)
        {
            foreach (var workflow in e.NewItems.OfType<WorkflowViewModel>())
            {
                AttachWorkflow(workflow);
            }
        }

        if (SelectedWorkflow != null && (Workflows == null || !Workflows.Contains(SelectedWorkflow)))
        {
            SelectedWorkflow = Workflows?.FirstOrDefault();
        }

        NotifyCatalogChanged();
    }

    private void OnWorkflowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        Validate();
        OnPropertyChanged(nameof(FilteredWorkflows));
        if (!_suppressAutoSave)
        {
            SaveAsync().Observe(ex => _logger?.LogWarning(ex, "Workflow auto-save failed"));
        }
    }

    private void OnWorkflowLifecycleEvent(WorkflowLifecycleEvent lifecycleEvent)
    {
        if (SelectedWorkflow == null || lifecycleEvent.WorkflowId == SelectedWorkflow.Id)
        {
            RefreshTrace();
        }
    }

    private void NotifyCatalogChanged()
    {
        OnPropertyChanged(nameof(Workflows));
        OnPropertyChanged(nameof(FilteredWorkflows));
        Validate();
        DuplicateSelectedWorkflowCommand.NotifyCanExecuteChanged();
        DeleteSelectedWorkflowCommand.NotifyCanExecuteChanged();
        AssignSelectedWorkflowCommand.NotifyCanExecuteChanged();
    }

    private Task SaveAsync() => _projectContext.SaveSolutionInternalAsync();

    private static Workflow Duplicate(Workflow source)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions.Compact);
        var duplicate = JsonSerializer.Deserialize<Workflow>(json, JsonOptions.Compact)
            ?? throw new InvalidOperationException("Workflow duplication could not deserialize the cloned graph.");
        duplicate.Id = Guid.NewGuid();
        duplicate.Steps ??= [];

        var idMap = new Dictionary<Guid, Guid>();
        var oldIds = duplicate.Steps.Select(step => step.Id).ToArray();
        for (var index = 0; index < duplicate.Steps.Count; index++)
        {
            var newId = Guid.NewGuid();
            idMap.TryAdd(oldIds[index], newId);
            duplicate.Steps[index].Id = newId;
            if (duplicate.Steps[index] is WorkflowActionStep { Action: { } action })
            {
                action.Id = Guid.NewGuid();
            }
        }

        duplicate.EntryStepId = Remap(duplicate.EntryStepId, idMap);
        RemapPolicy(duplicate.DefaultErrorPolicy, idMap);
        foreach (var step in duplicate.Steps)
        {
            step.NextStepId = Remap(step.NextStepId, idMap);
            RemapPolicy(step.ErrorPolicy, idMap);
            switch (step)
            {
                case WorkflowConditionStep condition:
                    condition.TrueStepId = Remap(condition.TrueStepId, idMap) ?? Guid.Empty;
                    condition.FalseStepId = Remap(condition.FalseStepId, idMap) ?? Guid.Empty;
                    break;
                case WorkflowParallelStep parallel:
                    parallel.JoinStepId = Remap(parallel.JoinStepId, idMap) ?? Guid.Empty;
                    foreach (var branch in parallel.Branches)
                    {
                        branch.EntryStepId = Remap(branch.EntryStepId, idMap) ?? Guid.Empty;
                    }
                    break;
            }
        }

        return duplicate;
    }

    private static void RemapPolicy(WorkflowErrorPolicy? policy, IReadOnlyDictionary<Guid, Guid> idMap)
    {
        if (policy != null)
        {
            policy.FailureStepId = Remap(policy.FailureStepId, idMap);
        }
    }

    private static Guid? Remap(Guid? id, IReadOnlyDictionary<Guid, Guid> idMap) =>
        id.HasValue && idMap.TryGetValue(id.Value, out var mapped) ? mapped : id;

    private static IReadOnlyList<WorkflowReference> FindReferences(Project project, Guid workflowId)
    {
        var references = new List<WorkflowReference>();
        foreach (var journey in project.Journeys)
        {
            for (var index = 0; index < journey.FeedbackSequence.Count; index++)
            {
                if (journey.FeedbackSequence[index].WorkflowId == workflowId)
                {
                    references.Add(new WorkflowReference("Journey", journey.Id, journey.Name, $"Feedback step {index + 1}"));
                }
            }
        }

        foreach (var workflow in project.Workflows)
        {
            foreach (var step in workflow.Steps?.OfType<WorkflowNestedStep>() ?? [])
            {
                if (step.WorkflowId == workflowId)
                {
                    references.Add(new WorkflowReference("Workflow", workflow.Id, workflow.Name, $"Nested step '{step.Name}'"));
                }
            }
        }

        return references;
    }

    private static string BuildReferenceMessage(string workflowName, IReadOnlyList<WorkflowReference> references)
    {
        var locations = string.Join(Environment.NewLine, references.Select(reference =>
            $"- {reference.OwnerType} '{reference.OwnerName}': {reference.Location}"));
        return $"Workflow '{workflowName}' cannot be deleted because it is referenced by:{Environment.NewLine}{locations}";
    }

    private static string CreateUniqueName(ProjectViewModel project, string baseName)
    {
        var name = baseName;
        var suffix = 2;
        while (project.Workflows.Any(workflow => string.Equals(workflow.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{baseName} {suffix++}";
        }

        return name;
    }
}
