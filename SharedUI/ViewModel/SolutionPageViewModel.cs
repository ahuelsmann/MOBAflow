// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

using System.ComponentModel;

/// <summary>
/// Owns the Solution page: adding and deleting projects and the persisted expansion of the project list and the
/// properties panel. The session stays the owner of the solution and of the project and journey selection.
/// </summary>
public sealed partial class SolutionPageViewModel : ObservableObject, IDisposable
{
    private readonly ISolutionSession _session;
    private readonly IDialogService _dialogService;
    private readonly LayoutPersistenceService _layout;
    private readonly ILogger<SolutionPageViewModel> _logger;
    private bool _isProjectListExpanded;
    private bool _isProjectPropertiesExpanded;
    private bool _disposed;

    /// <summary>Creates the Solution page state.</summary>
    /// <param name="session">Owns the solution, its persistence and the project and journey selection.</param>
    /// <param name="dialogService">Asks before a project is deleted.</param>
    /// <param name="layout">Persists the project list and properties panel expansion.</param>
    /// <param name="logger">Logs a backup that could not be written before a deletion.</param>
    public SolutionPageViewModel(
        ISolutionSession session,
        IDialogService dialogService,
        LayoutPersistenceService layout,
        ILogger<SolutionPageViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(logger);
        _session = session;
        _dialogService = dialogService;
        _layout = layout;
        _logger = logger;
        _isProjectListExpanded = layout.Layout.SolutionPage.IsProjectListExpanded;
        _isProjectPropertiesExpanded = layout.Layout.SolutionPage.IsPropertiesExpanded;
        _session.PropertyChanged += OnSessionPropertyChanged;
    }

    /// <summary>Gets the view model of the loaded solution whose projects the page lists.</summary>
    public SolutionViewModel? SolutionViewModel => _session.SolutionViewModel;

    /// <summary>Gets or sets the selected project; the session owns the selection.</summary>
    public ProjectViewModel? SelectedProject
    {
        get => _session.SelectedProject;
        set => _session.SelectedProject = value;
    }

    public bool IsProjectListExpanded
    {
        get => _isProjectListExpanded;
        set
        {
            if (SetProperty(ref _isProjectListExpanded, value))
                _layout.Persist(layout => layout.SolutionPage.IsProjectListExpanded = value);
        }
    }

    public bool IsProjectPropertiesExpanded
    {
        get => _isProjectPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isProjectPropertiesExpanded, value))
                _layout.Persist(layout => layout.SolutionPage.IsPropertiesExpanded = value);
        }
    }

    /// <summary>Creates a new empty project in the current solution and selects it.</summary>
    [RelayCommand]
    private void AddProject()
    {
        _session.AddProject(new Project { Name = "New Project" });
        DeleteProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProject))]
    private async Task DeleteProjectAsync()
    {
        if (SelectedProject is not { } deletedProject) return;

        // Show confirmation dialog; a connected runtime stops its trains when the project goes away.
        var stopsTrains = _session.GetConnectedProjectNames([deletedProject.Model.Id]).Count > 0;
        var confirmed = await _dialogService.ShowConfirmationAsync(
            title: "Delete Project",
            message: stopsTrains
                ? "Do you really want to delete the project? Its runtime is connected to a Z21; "
                    + "all its locomotives are set to speed 0 first."
                : "Do you really want to delete the project?",
            confirmButtonText: "Yes",
            cancelButtonText: "No",
            isCancelDefault: true).ConfigureAwait(true);

        if (!confirmed) return;

        BackUpSolutionFile();

        // Clear the detail selections first so no page shows stale data from the deleted project.
        _session.SelectedProject = null;
        _session.SelectedJourney = null;

        // Removes the project and selects the first remaining one.
        _session.RemoveProject(deletedProject);

        DeleteProjectCommand.NotifyCanExecuteChanged();
    }

    private bool CanDeleteProject() => SelectedProject != null;

    /// <summary>Copies the solution file next to itself before a deletion; a failed copy does not stop the deletion.</summary>
    private void BackUpSolutionFile()
    {
        var solutionPath = _session.CurrentSolutionPath;
        if (string.IsNullOrEmpty(solutionPath) || !File.Exists(solutionPath))
        {
            return;
        }

        try
        {
            var dir = Path.GetDirectoryName(solutionPath);
            var fileName = Path.GetFileNameWithoutExtension(solutionPath);
            var ext = Path.GetExtension(solutionPath);
            var backupPath = Path.Combine(dir ?? string.Empty, $"{fileName}.backup{ext}");
            File.Copy(solutionPath, backupPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
            or ArgumentException)
        {
            // The user confirmed the deletion; it proceeds without the backup.
            LogBackupFailed(_logger, ex);
        }
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ISolutionSession.SelectedProject):
                OnPropertyChanged(nameof(SelectedProject));
                break;
            case nameof(ISolutionSession.SolutionViewModel):
                OnPropertyChanged(nameof(SolutionViewModel));
                break;
        }
    }

    /// <summary>Stops following the session once shutdown is confirmed.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.PropertyChanged -= OnSessionPropertyChanged;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to create backup before project deletion")]
    private static partial void LogBackupFailed(ILogger logger, Exception exception);
}
