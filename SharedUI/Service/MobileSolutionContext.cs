// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using CommunityToolkit.Mvvm.ComponentModel;

using Domain;

using Interface;

using ViewModel;

/// <summary>
/// Holds the solution fetched from MOBApi for MOBAsmart train control and related views.
/// </summary>
public sealed class MobileSolutionContext : ObservableObject, IProjectContext
{
    private SolutionViewModel? _solutionViewModel;
    private ProjectViewModel? _selectedProject;

    /// <inheritdoc />
    public SolutionViewModel? SolutionViewModel => _solutionViewModel;

    /// <inheritdoc />
    public SolutionSaveState SolutionSaveState => SolutionSaveState.NotSaved;

    /// <inheritdoc />
    public string SolutionSaveStatusText => "Not saved";

    /// <inheritdoc />
    public ProjectViewModel? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (ReferenceEquals(_selectedProject, value))
            {
                return;
            }

            _selectedProject = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public JourneyViewModel? SelectedJourney { get; set; }

    /// <summary>
    /// Rebuilds view models from a freshly synced solution and selects the active MOBAflow project.
    /// </summary>
    public void ApplySolution(Solution solution, string? activeProjectName = null)
    {
        ArgumentNullException.ThrowIfNull(solution);

        _solutionViewModel = new SolutionViewModel(solution);
        _selectedProject = ResolveProjectViewModel(_solutionViewModel, activeProjectName);
        SelectedJourney = null;

        OnPropertyChanged(nameof(SolutionViewModel));
        OnPropertyChanged(nameof(SelectedProject));
    }

    private static ProjectViewModel? ResolveProjectViewModel(
        SolutionViewModel solutionViewModel,
        string? activeProjectName)
    {
        if (!string.IsNullOrWhiteSpace(activeProjectName))
        {
            var match = solutionViewModel.Projects.FirstOrDefault(project =>
                string.Equals(project.Name, activeProjectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }
        }

        return solutionViewModel.Projects.FirstOrDefault();
    }

    /// <inheritdoc />
    public Task SaveSolutionInternalAsync() => Task.CompletedTask;
}
