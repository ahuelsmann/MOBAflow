// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Backend.Model;
using Moba.Backend.Model.Action;
using Moba.SharedUI.Service;
using System.Collections.ObjectModel;
using BackendAction = Moba.Backend.Model.Action.Base;

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

    private ObservableCollection<BackendAction> _actions;
    
    /// <summary>
    /// Gets the Actions collection for the selected Workflow.
    /// </summary>
    public ObservableCollection<BackendAction> Actions
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
        _actions = new ObservableCollection<BackendAction>();
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

        var newAction = new Announcement("New Announcement")
        {
            Name = "New Announcement"
        };

        SelectedWorkflow.Actions.Add(newAction);
        Actions.Add(newAction);
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new Command(new byte[] { 0x00 })
        {
            Name = "New Command"
        };

        SelectedWorkflow.Actions.Add(newAction);
        Actions.Add(newAction);
    }

    [RelayCommand]
    private void AddAudio()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new Audio("sound.wav")
        {
            Name = "New Audio"
        };

        SelectedWorkflow.Actions.Add(newAction);
        Actions.Add(newAction);
    }

    partial void OnSelectedWorkflowChanged(Workflow? value)
    {
        // Update Actions collection when Workflow selection changes
        Actions.Clear();
        if (value != null)
        {
            foreach (var action in value.Actions)
            {
                if (action is BackendAction backendAction)
                {
                    Actions.Add(backendAction);
                }
            }
        }
        ValidationError = null;
        
        // Notify Delete command that CanExecute might have changed
        DeleteWorkflowCommand.NotifyCanExecuteChanged();
    }
}
