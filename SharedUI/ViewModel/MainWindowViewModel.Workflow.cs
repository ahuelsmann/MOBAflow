// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Action;

using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

/// <summary>
/// MainWindowViewModel - Workflow Management
/// Handles Workflow CRUD operations and Workflow Actions (Announcement, Command, Audio).
/// </summary>
public partial class MainWindowViewModel
{
    #region Workflow Search/Filter
    /// <summary>
    /// Gets or sets the search text used to filter workflows by name on the Workflows page.
    /// </summary>
    public string WorkflowSearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                WorkflowLibrary.SearchText = value;
                OnPropertyChanged(nameof(FilteredWorkflows));
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets the filtered workflows based on search text.
    /// Returns all workflows if search is empty.
    /// </summary>
    public List<WorkflowViewModel> FilteredWorkflows
    {
        get
        {
            if (SelectedProject == null)
                return [];

            return [.. WorkflowLibrary.FilteredWorkflows];
        }
    }
    #endregion

    #region Workflow CRUD Commands
    [RelayCommand]
    private void AddWorkflow()
    {
        WorkflowLibrary.CreateWorkflowCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWorkflow))]
    private void DeleteWorkflow()
    {
        WorkflowLibrary.SelectedWorkflow = SelectedWorkflow;
        WorkflowLibrary.DeleteSelectedWorkflowCommand.Execute(null);
    }

    private bool CanDeleteWorkflow() => SelectedWorkflow != null;

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        if (WorkflowLibrary.SelectedWorkflow != value)
        {
            WorkflowLibrary.SelectedWorkflow = value;
        }
    }

    private void OnWorkflowLibraryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = sender;
        if (e.PropertyName == nameof(WorkflowLibraryViewModel.SelectedWorkflow)
            && SelectedWorkflow != WorkflowLibrary.SelectedWorkflow)
        {
            SelectedWorkflow = WorkflowLibrary.SelectedWorkflow;
        }

        if (e.PropertyName is nameof(WorkflowLibraryViewModel.FilteredWorkflows)
            or nameof(WorkflowLibraryViewModel.Workflows))
        {
            OnPropertyChanged(nameof(FilteredWorkflows));
        }
    }
    #endregion

    #region Workflow Actions Commands
    [RelayCommand]
    private void AddAnnouncement()
    {
        if (SelectedWorkflow == null) return;

        SelectedWorkflow.AddActionCommand.Execute(ActionType.Announcement);

        // Trigger auto-save after adding action
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (SelectedWorkflow == null) return;

        SelectedWorkflow.AddActionCommand.Execute(ActionType.Command);

        // Trigger auto-save after adding action
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddAudio()
    {
        if (SelectedWorkflow == null) return;

        SelectedWorkflow.AddActionCommand.Execute(ActionType.Audio);

        // Trigger auto-save after adding action
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddSelectSignalAspect()
    {
        if (SelectedWorkflow == null) return;

        SelectedWorkflow.AddActionCommand.Execute(ActionType.SelectSignalAspect);

        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddExecuteScript()
    {
        if (SelectedWorkflow == null) return;

        SelectedWorkflow.AddActionCommand.Execute(ActionType.ExecuteScript);

        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddTrainDestinationDisplay()
    {
        if (SelectedWorkflow == null) return;

        SelectedWorkflow.AddActionCommand.Execute(ActionType.TrainDestinationDisplay);

        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddChangeJourneyStop()
    {
        if (SelectedWorkflow == null) return;
        SelectedWorkflow.AddActionCommand.Execute(ActionType.ChangeJourneyStop);
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAction))]
    private void DeleteAction()
    {
        if (SelectedWorkflow == null || SelectedAction == null) return;

        var actionVm = SelectedAction as WorkflowActionViewModel;
        if (actionVm == null) return;

        // Find and remove action from Domain model by ID
        var action = SelectedWorkflow.Model.Actions
            .FirstOrDefault(a => a.Id == actionVm.Id);

        if (action != null)
        {
            SelectedWorkflow.Model.Actions.Remove(action);
        }

        // Remove from ViewModel's ObservableCollection
        SelectedWorkflow.Actions.Remove(actionVm);
        SelectedAction = null;

        // Trigger auto-save after deleting action
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    private bool CanDeleteAction() => SelectedAction != null;
    #endregion
}
