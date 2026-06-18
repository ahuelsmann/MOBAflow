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
    /// Rebuilds view models from a freshly synced solution and selects the first project.
    /// </summary>
    public void ApplySolution(Solution solution)
    {
        ArgumentNullException.ThrowIfNull(solution);

        _solutionViewModel = new SolutionViewModel(solution);
        _selectedProject = _solutionViewModel.Projects.FirstOrDefault();
        SelectedJourney = null;

        OnPropertyChanged(nameof(SolutionViewModel));
        OnPropertyChanged(nameof(SelectedProject));
    }

    /// <inheritdoc />
    public Task SaveSolutionInternalAsync() => Task.CompletedTask;
}