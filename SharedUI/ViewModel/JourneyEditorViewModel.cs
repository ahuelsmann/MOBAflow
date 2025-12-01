// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Domain;
using Moba.Domain.Enum;
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
    private readonly ICityLibraryService? _cityLibraryService;

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

    [ObservableProperty]
    private ObservableCollection<City> _availableCities = [];

    [ObservableProperty]
    private string _citySearchText = string.Empty;

    /// <summary>
    /// Available behavior options for when the journey reaches the last station.
    /// </summary>
    public Array BehaviorOptions => Enum.GetValues(typeof(BehaviorOnLastStop));

    public JourneyEditorViewModel(Project project, ValidationService? validationService = null, ICityLibraryService? cityLibraryService = null)
    {
        _project = project;
        _validationService = validationService;
        _cityLibraryService = cityLibraryService;
        _journeys = new ObservableCollection<Journey>(project.Journeys);
        _stations = new ObservableCollection<Station>();

        // Load city library asynchronously
        _ = LoadCitiesAsync();
    }

    private async Task LoadCitiesAsync()
    {
        if (_cityLibraryService == null) return;

        var cities = await _cityLibraryService.LoadCitiesAsync();
        AvailableCities = new ObservableCollection<City>(cities);
    }

    partial void OnCitySearchTextChanged(string value)
    {
        if (_cityLibraryService == null) return;

        var filtered = _cityLibraryService.FilterCities(value);
        AvailableCities = new ObservableCollection<City>(filtered);
    }

    [RelayCommand]
    private void AddJourney()
    {
        var newJourney = new Journey
        {
            Name = "New Journey",
            BehaviorOnLastStop = BehaviorOnLastStop.None
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

    private bool CanAddStation() => SelectedJourney != null;

    [RelayCommand(CanExecute = nameof(CanAddStation))]
    private void AddStation()
    {
        if (SelectedJourney == null) return;

        var newStation = new Station
        {
            Name = "New Station",
            NumberOfLapsToStop = 2
        };

        SelectedJourney.Stations.Add(newStation);
        Stations.Add(newStation);
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanAddStation))]
    private void AddStationFromCity(City city)
    {
        if (SelectedJourney == null || city == null) return;

        // Create Station from City's first station (Hauptbahnhof)
        var cityStation = city.Stations.FirstOrDefault();
        if (cityStation == null) return;

        var newStation = new Station
        {
            Name = cityStation.Name,
            Track = cityStation.Track,
            NumberOfLapsToStop = 1,
            Description = $"From {city.Name}",
            IsExitOnLeft = cityStation.IsExitOnLeft
        };

        SelectedJourney.Stations.Add(newStation);
        Stations.Add(newStation);
        SelectedStation = newStation; // Select newly added station
        ValidationError = null;

        System.Diagnostics.Debug.WriteLine($"✅ Added station from city: {city.Name} → {newStation.Name}");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteStation))]
    private void DeleteStation()
    {
        if (SelectedJourney == null || SelectedStation == null) return;

        // Remove from Journey model
        SelectedJourney.Stations.Remove(SelectedStation);
        
        // Remove from ViewModel collection
        Stations.Remove(SelectedStation);
        
        // Note: Station.Number property removed in Clean Architecture refactoring
        // Stations are now identified by their position in the list
        
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
