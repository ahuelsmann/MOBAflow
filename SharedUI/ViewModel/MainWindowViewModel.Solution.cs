// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Moba.Domain;

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
            OnPropertyChanged(nameof(CurrentProjectViewModel));
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
        OnPropertyChanged(nameof(CurrentProjectViewModel));
        
        LoadCities();
    }

    [RelayCommand(CanExecute = nameof(CanSaveSolution))]
    private async Task SaveSolutionAsync()
    {
        var (success, path, error) = await _ioService.SaveAsync(Solution, CurrentSolutionPath);
        if (success && path != null)
        {
            CurrentSolutionPath = path;
            HasUnsavedChanges = false;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Failed to save solution: {error}");
        }
    }

    [RelayCommand]
    private async Task NewSolutionAsync()
    {
        var (success, userCancelled, error) = await _ioService.NewSolutionAsync(HasUnsavedChanges);

        if (userCancelled)
        {
            return;
        }

        if (!success)
        {
            if (error == "SAVE_REQUESTED")
            {
                if (SaveSolutionCommand.CanExecute(null))
                {
                    await SaveSolutionCommand.ExecuteAsync(null);
                }
            }
            else
            {
                return;
            }
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
        HasUnsavedChanges = true;

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
            HasUnsavedChanges = false;
            HasSolution = Solution.Projects.Count > 0;

            SaveSolutionCommand.NotifyCanExecuteChanged();
            ConnectToZ21Command.NotifyCanExecuteChanged();
            LoadCities();

            OnPropertyChanged(nameof(Solution));
        }
    }

    private bool CanSaveSolution() => true;

    /// <summary>
    /// Loads cities from the first project into AvailableCities for UI binding.
    /// </summary>
    private void LoadCities()
    {
        if (Solution == null || Solution.Projects.Count == 0)
        {
            AvailableCities.Clear();
            return;
        }

        var cities = Solution.Projects[0].Cities;
        AvailableCities.Clear();

        foreach (var city in cities)
        {
            AvailableCities.Add(city);
        }

        System.Diagnostics.Debug.WriteLine($"✅ Loaded {cities.Count} cities into AvailableCities");
    }

    /// <summary>
    /// Loads cities from JSON library into the first project using CityService.
    /// </summary>
    private async Task LoadCitiesFromCityManagerAsync()
    {
        if (_cityLibraryService == null)
        {
            System.Diagnostics.Debug.WriteLine("❌ CityService not available");
            return;
        }

        try
        {
            // Load cities from JSON using CityService (Domain.City)
            var cities = await _cityLibraryService.LoadCitiesAsync();
            
            var firstProject = Solution.Projects[0];
            firstProject.Cities.Clear();
            
            // Add loaded cities to project
            foreach (var city in cities)
            {
                firstProject.Cities.Add(city);
            }
            
            // Refresh AvailableCities for UI binding
            LoadCities();
            
            System.Diagnostics.Debug.WriteLine($"✅ Loaded {cities.Count} cities from library");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to load cities: {ex.Message}");
            throw new InvalidOperationException($"Failed to load cities: {ex.Message}");
        }
    }

    #endregion
}