// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Action;

using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Helper;

using Microsoft.Extensions.Logging;

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

            var workflows = SelectedProject.Workflows;

            return string.IsNullOrWhiteSpace(WorkflowSearchText)
                ? [.. workflows]
                : [.. workflows.Where(w => w.Name.Contains(WorkflowSearchText, StringComparison.OrdinalIgnoreCase))];
        }
    }
    #endregion

    #region Workflow CRUD Commands
    [RelayCommand]
    private void AddWorkflow()
    {
        if (SelectedProject == null) return;

        var workflow = EntityEditorHelper.AddEntity(
            SelectedProject.Model.Workflows,
            SelectedProject.Workflows,
            () => new Workflow { Name = "New Workflow" },
            model => new WorkflowViewModel(model, ioService: _ioService, soundPlayer: _executionContext.SoundPlayer, loggerFactory: _loggerFactory));

        // Subscribe to PropertyChanged for auto-save (consistent with other ViewModels)
        workflow.PropertyChanged += OnViewModelPropertyChanged;

        SelectedWorkflow = workflow;
        OnPropertyChanged(nameof(FilteredWorkflows));

        // Trigger auto-save after adding workflow
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWorkflow))]
    private void DeleteWorkflow()
    {
        if (SelectedProject == null) return;

        // Unsubscribe from PropertyChanged events before deleting
        if (SelectedWorkflow != null)
        {
            SelectedWorkflow.PropertyChanged -= OnViewModelPropertyChanged;
        }

        EntityEditorHelper.DeleteEntity(
            SelectedWorkflow,
            SelectedProject.Model.Workflows,
            SelectedProject.Workflows,
            () => SelectedWorkflow = null);

        OnPropertyChanged(nameof(FilteredWorkflows));

        // Trigger auto-save after deleting workflow
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
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
            Type = ActionType.Announcement,
            Announcement = new AnnouncementActionPayload
            {
                Message = "Enter announcement text",
                VoiceName = "de-DE-KatjaNeural"
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new AnnouncementViewModel(newAction);
        SelectedWorkflow.Actions.Add(viewModel);

        // Trigger auto-save after adding action
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Command",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = ActionType.Command,
            Command = new CommandActionPayload
            {
                BytesBase64 = Convert.ToBase64String(new byte[] { 0x00 })
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new CommandViewModel(newAction, _loggerFactory?.CreateLogger<CommandViewModel>());
        SelectedWorkflow.Actions.Add(viewModel);

        // Trigger auto-save after adding action
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    [RelayCommand]
    private void AddAudio()
    {
        if (SelectedWorkflow == null) return;

        var newAction = new WorkflowAction
        {
            Name = "New Audio",
            Number = (uint)(SelectedWorkflow.Model.Actions.Count + 1),
            Type = ActionType.Audio,
            Audio = new AudioActionPayload
            {
                FilePath = "sound.wav"
            }
        };

        SelectedWorkflow.Model.Actions.Add(newAction);
        var viewModel = new AudioViewModel(newAction, _ioService, _executionContext.SoundPlayer);
        SelectedWorkflow.Actions.Add(viewModel);

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
