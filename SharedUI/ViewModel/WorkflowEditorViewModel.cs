// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Service;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Workflows tab in the Editor.
/// Master-Detail editor for Workflows and their Actions.
/// </summary>
public partial class WorkflowEditorViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly ValidationService? _validationService;

    [ObservableProperty]
    private ObservableCollection<Workflow> _workflows;

    [ObservableProperty]
    private Workflow? _selectedWorkflow;

    private ObservableCollection<Action.WorkflowActionViewModel> _actions;
    
    /// <summary>
    /// Gets the Actions collection for the selected Workflow.
    /// </summary>
    public ObservableCollection<Action.WorkflowActionViewModel> Actions
    {
        get => _actions;
        set => SetProperty(ref _actions, value);
    }

    [ObservableProperty]
    private string? _validationError;

    public WorkflowEditorViewModel(Project project, ValidationService? validationService = null)
    {
        _project = project;
        _validationService = validationService;
        _workflows = new ObservableCollection<Workflow>(project.Workflows);
        _actions = new ObservableCollection<Action.WorkflowActionViewModel>();
    }

    [RelayCommand]
    private void AddWorkflow()
    {
        var newWorkflow = new Workflow
        {
            Name = "New Workflow"
        };
        
        _project.Workflows.Add(newWorkflow);
        Workflows.Add(newWorkflow);
        SelectedWorkflow = newWorkflow;
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWorkflow))]
    private void DeleteWorkflow()
    {
        if (SelectedWorkflow == null) return;

        // Validate deletion
        if (_validationService != null)
        {
            var validationResult = _validationService.CanDeleteWorkflow(SelectedWorkflow);
            if (!validationResult.IsValid)
            {
                ValidationError = validationResult.ErrorMessage;
                return;
            }
        }

        _project.Workflows.Remove(SelectedWorkflow);
        Workflows.Remove(SelectedWorkflow);
        SelectedWorkflow = null;
        ValidationError = null;
    }

    private bool CanDeleteWorkflow() => SelectedWorkflow != null;

    [RelayCommand]
    private void AddAnnouncement()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Announcement",
            Number = SelectedWorkflow.Actions.Count + 1,
            Type = ActionType.Announcement,
            Parameters = new Dictionary<string, object>
            {
                ["Message"] = "Enter announcement text",
                ["VoiceName"] = "de-DE-KatjaNeural"
            }
        };

        SelectedWorkflow.Actions.Add(newAction);
        var viewModel = CreateActionViewModel(newAction);
        Actions.Add(viewModel);
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Command",
            Number = SelectedWorkflow.Actions.Count + 1,
            Type = ActionType.Command,
            Parameters = new Dictionary<string, object>
            {
                ["Bytes"] = new byte[] { 0x00 }
            }
        };

        SelectedWorkflow.Actions.Add(newAction);
        var viewModel = CreateActionViewModel(newAction);
        Actions.Add(viewModel);
    }

    [RelayCommand]
    private void AddAudio()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Audio",
            Number = SelectedWorkflow.Actions.Count + 1,
            Type = ActionType.Audio,
            Parameters = new Dictionary<string, object>
            {
                ["FilePath"] = "sound.wav"
            }
        };

        SelectedWorkflow.Actions.Add(newAction);
        var viewModel = CreateActionViewModel(newAction);
        Actions.Add(viewModel);
    }

    partial void OnSelectedWorkflowChanged(Workflow? value)
    {
        // Update Actions collection when Workflow selection changes
        Actions.Clear();
        if (value != null)
        {
            foreach (var action in value.Actions)
            {
                var viewModel = CreateActionViewModel(action);
                Actions.Add(viewModel);
            }
        }
        ValidationError = null;
        
        // Notify Delete command that CanExecute might have changed
        DeleteWorkflowCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Creates the appropriate ViewModel wrapper for a WorkflowAction based on its Type.
    /// </summary>
    private Action.WorkflowActionViewModel CreateActionViewModel(WorkflowAction action)
    {
        return action.Type switch
        {
            ActionType.Command => new Action.CommandViewModel(action),
            ActionType.Audio => new Action.AudioViewModel(action),
            ActionType.Announcement => new Action.AnnouncementViewModel(action),
            _ => throw new ArgumentException($"Unknown action type: {action.Type}")
        };
    }
}
