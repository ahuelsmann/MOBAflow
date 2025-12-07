// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Domain;
using Moba.SharedUI.Helper;

using System.Collections.Generic;

/// <summary>
/// MainWindowViewModel - Workflow Management
/// Handles Workflow CRUD operations and Workflow Actions (Announcement, Command, Audio).
/// </summary>
public partial class MainWindowViewModel
{
    #region Workflow Search/Filter

    private string _workflowSearchText = string.Empty;
    public string WorkflowSearchText
    {
        get => _workflowSearchText;
        set
        {
            if (SetProperty(ref _workflowSearchText, value))
            {
                OnPropertyChanged(nameof(FilteredWorkflows));
            }
        }
    }

    /// <summary>
    /// Gets the filtered workflows based on search text.
    /// Returns all workflows if search is empty.
    /// </summary>
    public List<WorkflowViewModel> FilteredWorkflows
    {
        get
        {
            if (CurrentProjectViewModel == null)
                return new List<WorkflowViewModel>();

            var workflows = CurrentProjectViewModel.Workflows;

            if (string.IsNullOrWhiteSpace(WorkflowSearchText))
                return workflows.ToList();

            return workflows
                .Where(w => w.Name.Contains(WorkflowSearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    #endregion

    #region Workflow CRUD Commands

    [RelayCommand]
    private void AddWorkflow()
    {
        if (CurrentProjectViewModel == null) return;

        var workflow = EntityEditorHelper.AddEntity(
            CurrentProjectViewModel.Model.Workflows,
            CurrentProjectViewModel.Workflows,
            () => new Workflow { Name = "New Workflow" },
            model => new WorkflowViewModel(model));

        SelectedWorkflow = workflow;
        HasUnsavedChanges = true;
        OnPropertyChanged(nameof(FilteredWorkflows));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWorkflow))]
    private void DeleteWorkflow()
    {
        if (CurrentProjectViewModel == null) return;

        EntityEditorHelper.DeleteEntity(
            SelectedWorkflow,
            CurrentProjectViewModel.Model.Workflows,
            CurrentProjectViewModel.Workflows,
            () => { SelectedWorkflow = null; HasUnsavedChanges = true; });
        
        OnPropertyChanged(nameof(FilteredWorkflows));
    }

    private bool CanDeleteWorkflow() => SelectedWorkflow != null;

    #endregion

    #region Workflow Actions Commands

    [RelayCommand]
    private void AddAnnouncement()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Announcement",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = Domain.Enum.ActionType.Announcement,
            Parameters = new Dictionary<string, object>
            {
                ["Message"] = "Enter announcement text",
                ["VoiceName"] = "de-DE-KatjaNeural"
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new Action.AnnouncementViewModel(newAction);
        SelectedWorkflow.Actions.Add(viewModel);

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Command",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = Domain.Enum.ActionType.Command,
            Parameters = new Dictionary<string, object>
            {
                ["Bytes"] = new byte[] { 0x00 }
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new Action.CommandViewModel(newAction);
        SelectedWorkflow.Actions.Add(viewModel);

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddAudio()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Audio",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = Domain.Enum.ActionType.Audio,
            Parameters = new Dictionary<string, object>
            {
                ["FilePath"] = "sound.wav"
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new Action.AudioViewModel(newAction);
        SelectedWorkflow.Actions.Add(viewModel);

        HasUnsavedChanges = true;
    }

    #endregion
}