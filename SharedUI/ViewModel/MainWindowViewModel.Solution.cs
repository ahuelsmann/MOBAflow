// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using System.ComponentModel;

/// <summary>
/// MainWindowViewModel - solution new/open/save.
/// The <see cref="ISolutionSession"/> owns the solution, its persistence and the project/journey selection;
/// this partial reads them through for the window title, the shell's commands and the runtime projection.
/// Project creation and deletion live in <see cref="SolutionPageViewModel"/>.
/// </summary>
public partial class MainWindowViewModel
{
    #region Session-backed state
    /// <summary>Gets the loaded solution.</summary>
    public Solution Solution => _session.Solution;

    /// <summary>Gets or sets the file the solution was loaded from or saved to.</summary>
    public string? CurrentSolutionPath
    {
        get => _session.CurrentSolutionPath;
        set => _session.CurrentSolutionPath = value;
    }

    /// <summary>Gets whether the solution has changes that were not written yet.</summary>
    public bool HasUnsavedChanges => _session.HasUnsavedChanges;

    /// <summary>Gets the view model of the loaded solution.</summary>
    public SolutionViewModel? SolutionViewModel => _session.SolutionViewModel;

    /// <summary>Gets or sets the selected project.</summary>
    public ProjectViewModel? SelectedProject
    {
        get => _session.SelectedProject;
        set => _session.SelectedProject = value;
    }

    /// <summary>Gets or sets the selected journey.</summary>
    public JourneyViewModel? SelectedJourney
    {
        get => _session.SelectedJourney;
        set => _session.SelectedJourney = value;
    }

    /// <summary>Gets the current non-interactive solution persistence state.</summary>
    public SolutionSaveState SolutionSaveState => _session.SolutionSaveState;

    /// <summary>Gets an actionable, non-modal description of the current persistence state.</summary>
    public string SolutionSaveStatusText => _session.SolutionSaveStatusText;

    private void AttachSolutionSession()
    {
        _session.PropertyChanged += OnSolutionSessionPropertyChanged;
        _session.SolutionReplacing += OnSolutionReplacing;
        _session.SolutionLoaded += OnSessionSolutionLoaded;
    }

    private void OnSolutionSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ISolutionSession.SelectedProject):
                OnPropertyChanged(nameof(SelectedProject));
                HandleSelectedProjectChanged();
                break;
            case nameof(ISolutionSession.SelectedJourney):
                OnPropertyChanged(nameof(SelectedJourney));
                break;
            case nameof(ISolutionSession.CurrentSolutionPath):
                OnPropertyChanged(nameof(CurrentSolutionPath));
                break;
            case nameof(ISolutionSession.HasUnsavedChanges):
            case nameof(ISolutionSession.SolutionViewModel):
            case nameof(ISolutionSession.SolutionSaveState):
            case nameof(ISolutionSession.SolutionSaveStatusText):
            case nameof(ISolutionSession.Solution):
                OnPropertyChanged(e.PropertyName);
                break;
        }
    }

    /// <summary>
    /// Called when SelectedProject changes: refreshes the journey runtime projection for the new project.
    /// Counter statistics, diagnostics and the solution-loaded status follow the selection in
    /// InPortCounterViewModel, ProjectDiagnosticsViewModel and SystemStatusViewModel.
    /// </summary>
    private void HandleSelectedProjectChanged() => ApplyJourneyRuntimeSnapshots(_latestRuntimeSnapshot.JourneyStates);

    private void OnSolutionReplacing(object? sender, EventArgs e) => ClearAllSelections();

    private void OnSessionSolutionLoaded(object? sender, EventArgs e)
    {
        this.SaveSolutionCommand.NotifyCanExecuteChanged();
        this.ConnectCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Solution Management
    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        await _session.SaveAsync(allowPathSelection: true);
    }

    /// <summary>
    /// Marks the solution as changed and persists it without opening a file picker.
    /// </summary>
    public Task SaveSolutionInternalAsync() => _session.SaveSolutionInternalAsync();

    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        if (await ConfirmStopTrainsAsync("Creating a new solution").ConfigureAwait(true))
        {
            await _session.NewSolutionAsync().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        if (await ConfirmStopTrainsAsync("Opening another solution").ConfigureAwait(true))
        {
            await _session.LoadSolutionAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Warns before an action discards runtimes that are connected to their Z21. Confirming lets the runtimes set
    /// their locomotives to speed 0 before they are discarded; cancelling keeps everything running.
    /// </summary>
    /// <returns><see langword="true"/> to continue the action.</returns>
    private async Task<bool> ConfirmStopTrainsAsync(string action, IReadOnlyCollection<Guid>? projectIds = null)
    {
        var connected = _session.GetConnectedProjectNames(projectIds);
        if (connected.Count == 0)
        {
            return true;
        }

        return await _dialogService.ShowConfirmationAsync(
            title: "Stop all trains?",
            message: $"{action} stops {DescribeRuntimes(connected)}. "
                + "All locomotives on the Z21 are set to speed 0 first.",
            confirmButtonText: "Stop trains",
            cancelButtonText: "Cancel",
            isCancelDefault: true).ConfigureAwait(true);
    }

    private static string DescribeRuntimes(IReadOnlyList<string> projectNames) => projectNames.Count == 1
        ? $"the runtime of project '{projectNames[0]}'"
        : $"the runtimes of projects {string.Join(", ", projectNames.Select(name => $"'{name}'"))}";

    private bool CanSaveSolution() => _session.CanSave;

    /// <summary>
    /// Clears the project and journey selection to reset the property panels when the session replaces the solution.
    /// </summary>
    private void ClearAllSelections()
    {
        // The shared workflow library follows the cleared project selection itself.
        SelectedProject = null;
        SelectedJourney = null;
    }

    #endregion
}
