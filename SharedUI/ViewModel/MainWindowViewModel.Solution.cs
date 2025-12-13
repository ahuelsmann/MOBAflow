// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Domain;

using System;
using System.Threading.Tasks;

/// <summary>
/// MainWindowViewModel - Solution and Project Management
/// Handles solution lifecycle (New, Load, Save) and project management.
/// </summary>
public partial class MainWindowViewModel
{
    #region Solution Management
    partial void OnSolutionChanged(Solution? value)
    {
        if (value == null)
        {
            HasSolution = false;
            SolutionViewModel = null;
            AvailableCities.Clear();
            OnPropertyChanged(nameof(SelectedProject));
            OnPropertyChanged(nameof(FilteredJourneys));
            OnPropertyChanged(nameof(FilteredWorkflows));
            return;
        }

        // Ensure Solution always has at least one project
        if (value.Projects.Count == 0)
        {
            value.Projects.Add(new Project { Name = "(Untitled Project)" });
        }

        SolutionViewModel = new SolutionViewModel(value, _uiDispatcher);
        HasSolution = value.Projects.Count > 0;

        // Auto-select first project if no project is selected
        if (SelectedProject == null && SolutionViewModel.Projects.Count > 0)
        {
            SelectedProject = SolutionViewModel.Projects[0];
        }

        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedProject));
        
        LoadCities();
    }

    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
        if (success && path != null)
        {
            CurrentSolutionPath = path;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to save solution: {error}");
        }
    }

    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        if (SaveSolutionCommand.CanExecute(null))
        {
            await SaveSolutionCommand.ExecuteAsync(null);
        }

        // Clear existing Solution (DI singleton)
        Solution.Projects.Clear();
        Solution.Name = "New Solution";

        // Add default project
        Solution.Projects.Add(new Project
        {
            Name = "New Project",
            Journeys = new List<Journey>(),
            Workflows = new List<Workflow>(),
            Trains = new List<Train>()
        });

        SolutionViewModel?.Refresh();

        CurrentSolutionPath = null;

        SaveSolutionCommand.NotifyCanExecuteChanged();
        ConnectToZ21Command.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        var (loadedSolution, path, error) = await _ioService.LoadAsync();

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to load solution: {error}");
        }

        if (loadedSolution != null)
        {
            Solution.Projects.Clear();
            foreach (var project in loadedSolution.Projects)
            {
                Solution.Projects.Add(project);
            }

            Solution.Name = loadedSolution.Name;
            SolutionViewModel?.Refresh();

            CurrentSolutionPath = path;
            HasSolution = Solution.Projects.Count > 0;

            SaveSolutionCommand.NotifyCanExecuteChanged();
            ConnectToZ21Command.NotifyCanExecuteChanged();
            LoadCities();

            OnPropertyChanged(nameof(Solution));
        }
    }

    private bool CanSaveSolution() => true;

    /// <summary>
    /// Loads cities from City Library into AvailableCities for UI binding.
    /// Cities are master data loaded from CityService, not stored in Project.
    /// </summary>
    private void LoadCities()
    {
        // Cities are loaded from CityLibrary, NOT from Project
        // This method can be removed or kept as no-op for backward compatibility
        System.Diagnostics.Debug.WriteLine("ℹ️ LoadCities called - Cities are loaded from CityLibrary on startup");
    }
    #endregion
}