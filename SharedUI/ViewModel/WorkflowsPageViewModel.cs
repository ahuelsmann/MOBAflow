// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Service;

/// <summary>
/// Owns the Workflows page presentation: the shared workflow library and the persisted panel expansion.
/// Workflow selection, action editing, validation, dry run and trace stay in <see cref="WorkflowLibraryViewModel"/>,
/// which the host registers once and shares with the station and event-manager surfaces.
/// </summary>
public sealed class WorkflowsPageViewModel : ObservableObject
{
    private readonly LayoutPersistenceService _layout;
    private bool _isWorkflowListExpanded;
    private bool _isWorkflowActionsExpanded;
    private bool _isWorkflowPropertiesExpanded;

    public WorkflowsPageViewModel(WorkflowLibraryViewModel workflowLibrary, LayoutPersistenceService layout)
    {
        ArgumentNullException.ThrowIfNull(workflowLibrary);
        ArgumentNullException.ThrowIfNull(layout);
        WorkflowLibrary = workflowLibrary;
        _layout = layout;
        _isWorkflowListExpanded = layout.Layout.WorkflowsPage.IsWorkflowListExpanded;
        _isWorkflowActionsExpanded = layout.Layout.WorkflowsPage.IsActionsExpanded;
        _isWorkflowPropertiesExpanded = layout.Layout.WorkflowsPage.IsPropertiesExpanded;
    }

    /// <summary>Gets the host-owned workflow catalog and editor state shared by every workflow surface.</summary>
    public WorkflowLibraryViewModel WorkflowLibrary { get; }

    public bool IsWorkflowListExpanded
    {
        get => _isWorkflowListExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowListExpanded, value))
                _layout.Persist(layout => layout.WorkflowsPage.IsWorkflowListExpanded = value);
        }
    }

    public bool IsWorkflowActionsExpanded
    {
        get => _isWorkflowActionsExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowActionsExpanded, value))
                _layout.Persist(layout => layout.WorkflowsPage.IsActionsExpanded = value);
        }
    }

    public bool IsWorkflowPropertiesExpanded
    {
        get => _isWorkflowPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowPropertiesExpanded, value))
                _layout.Persist(layout => layout.WorkflowsPage.IsPropertiesExpanded = value);
        }
    }
}
