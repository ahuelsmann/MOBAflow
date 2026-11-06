// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using Common.Extension;
using CommunityToolkit.Mvvm.ComponentModel;
using Interface;
using Microsoft.Extensions.Logging;

/// <summary>
/// Owns the Workflows page presentation: the shared workflow library and the persisted panel expansion.
/// Workflow selection, action editing, validation, dry run and trace stay in <see cref="WorkflowLibraryViewModel"/>,
/// which the host registers once and shares with the station and event-manager surfaces.
/// </summary>
public sealed partial class WorkflowsPageViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ILogger<WorkflowsPageViewModel> _logger;
    private bool _isWorkflowListExpanded;
    private bool _isWorkflowActionsExpanded;
    private bool _isWorkflowPropertiesExpanded;

    public WorkflowsPageViewModel(WorkflowLibraryViewModel workflowLibrary, ISettingsService settingsService,
        ILogger<WorkflowsPageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(workflowLibrary);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);
        WorkflowLibrary = workflowLibrary;
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();
        _logger = logger;
        _isWorkflowListExpanded = _settings.Layout.WorkflowsPage.IsWorkflowListExpanded;
        _isWorkflowActionsExpanded = _settings.Layout.WorkflowsPage.IsActionsExpanded;
        _isWorkflowPropertiesExpanded = _settings.Layout.WorkflowsPage.IsPropertiesExpanded;
    }

    /// <summary>Gets the host-owned workflow catalog and editor state shared by every workflow surface.</summary>
    public WorkflowLibraryViewModel WorkflowLibrary { get; }

    public bool IsWorkflowListExpanded
    {
        get => _isWorkflowListExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowListExpanded, value))
                PersistLayoutState(layout => layout.WorkflowsPage.IsWorkflowListExpanded = value);
        }
    }

    public bool IsWorkflowActionsExpanded
    {
        get => _isWorkflowActionsExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowActionsExpanded, value))
                PersistLayoutState(layout => layout.WorkflowsPage.IsActionsExpanded = value);
        }
    }

    public bool IsWorkflowPropertiesExpanded
    {
        get => _isWorkflowPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowPropertiesExpanded, value))
                PersistLayoutState(layout => layout.WorkflowsPage.IsPropertiesExpanded = value);
        }
    }

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        _settingsService.SaveSettingsAsync(_settings)
            .Observe(ex => LogOperationFailed(_logger, "Persist workflow layout", ex));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{OperationName} failed")]
    private static partial void LogOperationFailed(ILogger logger, string operationName, Exception exception);
}
