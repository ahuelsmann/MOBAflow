// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Backend.Model;
using Moba.SharedUI.Service;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Journeys tab in the Editor.
/// Master-Detail editor for Journeys and their Stations.
/// </summary>
public partial class JourneyEditorViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly ValidationService? _validationService;

    [ObservableProperty]
    private ObservableCollection<Journey> _journeys;

    [ObservableProperty]
    private Journey? _selectedJourney;

    [ObservableProperty]
    private ObservableCollection<Station> _stations;

    [ObservableProperty]
    private Station? _selectedStation;

    [ObservableProperty]
    private string? _validationError;

    public JourneyEditorViewModel(Project project, ValidationService? validationService = null)
    {
        _project = project;
        _validationService = validationService;
        _journeys = new ObservableCollection<Journey>(project.Journeys);
        _stations = new ObservableCollection<Station>();
    }

    [RelayCommand]
    private void AddJourney()
    {
        var newJourney = new Journey
        {
            Name = "New Journey",
            OnLastStop = Backend.Model.Enum.BehaviorOnLastStop.None
        };
        
        _project.Journeys.Add(newJourney);
        Journeys.Add(newJourney);
        SelectedJourney = newJourney;
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteJourney))]
    private void DeleteJourney()
    {
        if (SelectedJourney == null) return;

        // Validate deletion
        if (_validationService != null)
        {
            var validationResult = _validationService.CanDeleteJourney(SelectedJourney);
            if (!validationResult.IsValid)
            {
                ValidationError = validationResult.ErrorMessage;
                return;
            }
        }

        _project.Journeys.Remove(SelectedJourney);
        Journeys.Remove(SelectedJourney);
        SelectedJourney = null;
        ValidationError = null;
    }

    private bool CanDeleteJourney() => SelectedJourney != null;

    [RelayCommand]
    private void AddStation()
    {
        if (SelectedJourney == null) return;

        var newStation = new Station
        {
            Name = "New Station",
            Number = (uint)(SelectedJourney.Stations.Count + 1)
        };

        SelectedJourney.Stations.Add(newStation);
        Stations.Add(newStation);
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteStation))]
    private void DeleteStation()
    {
        if (SelectedJourney == null || SelectedStation == null) return;

        // Remove from Journey model
        SelectedJourney.Stations.Remove(SelectedStation);
        
        // Remove from ViewModel collection
        Stations.Remove(SelectedStation);
        
        // Renumber remaining stations
        for (int i = 0; i < SelectedJourney.Stations.Count; i++)
        {
            SelectedJourney.Stations[i].Number = (uint)(i + 1);
        }
        
        SelectedStation = null;
        ValidationError = null;
    }

    private bool CanDeleteStation() => SelectedStation != null && SelectedJourney != null;

    partial void OnSelectedJourneyChanged(Journey? value)
    {
        // Update Stations collection when Journey selection changes
        Stations.Clear();
        if (value != null)
        {
            foreach (var station in value.Stations)
            {
                Stations.Add(station);
            }
        }
        ValidationError = null;
        
        // Notify commands that CanExecute might have changed
        DeleteJourneyCommand.NotifyCanExecuteChanged();
        DeleteStationCommand.NotifyCanExecuteChanged();
    }
    
    partial void OnSelectedStationChanged(Station? value)
    {
        // Notify Delete command that CanExecute might have changed
        DeleteStationCommand.NotifyCanExecuteChanged();
    }
}
