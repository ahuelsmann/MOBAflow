// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Helper;

/// <summary>
/// MainWindowViewModel - Journey and Station Management
/// Handles Journey and Station CRUD operations.
/// </summary>
public partial class MainWindowViewModel
{
    #region Journey Factory

    /// <summary>
    /// Creates a JourneyViewModel with SessionState.
    /// Falls back to simplified constructor if JourneyManager is not initialized (e.g., in tests).
    /// </summary>
    private JourneyViewModel CreateJourneyViewModel(Journey journey)
    {
        // Fallback for tests or when JourneyManager not initialized
        if (_journeyManager == null)
        {
            return new JourneyViewModel(journey, _uiDispatcher);
        }

        var state = _journeyManager.GetState(journey.Id);
        
        // If state doesn't exist yet (journey just created), create dummy state
        if (state == null)
        {
            return new JourneyViewModel(journey, _uiDispatcher);
        }

        return new JourneyViewModel(journey, state, _journeyManager, _uiDispatcher);
    }

    #endregion

    #region Journey Search/Filter

    private string _journeySearchText = string.Empty;
    public string JourneySearchText
    {
        get => _journeySearchText;
        set
        {
            if (SetProperty(ref _journeySearchText, value))
            {
                OnPropertyChanged(nameof(FilteredJourneys));
            }
        }
    }

    /// <summary>
    /// Gets the filtered journeys based on search text.
    /// Returns all journeys if search is empty.
    /// </summary>
    public List<JourneyViewModel> FilteredJourneys
    {
        get
        {
            if (CurrentProjectViewModel == null)
                return new List<JourneyViewModel>();

            var journeys = CurrentProjectViewModel.Journeys;

            if (string.IsNullOrWhiteSpace(JourneySearchText))
                return journeys.ToList();

            return journeys
                .Where(j => j.Name.Contains(JourneySearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    #endregion

    #region Journey CRUD Commands

    [RelayCommand]
    private void AddJourney()
    {
        if (CurrentProjectViewModel == null) return;

        var journey = EntityEditorHelper.AddEntity(
            CurrentProjectViewModel.Model.Journeys,
            CurrentProjectViewModel.Journeys,
            () => new Journey { Name = "New Journey", BehaviorOnLastStop = BehaviorOnLastStop.None },
            model => CreateJourneyViewModel(model));

        SelectedJourney = journey;
        HasUnsavedChanges = true;
        OnPropertyChanged(nameof(FilteredJourneys));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteJourney))]
    private void DeleteJourney()
    {
        if (CurrentProjectViewModel == null) return;

        EntityEditorHelper.DeleteEntity(
            SelectedJourney,
            CurrentProjectViewModel.Model.Journeys,
            CurrentProjectViewModel.Journeys,
            () => { SelectedJourney = null; HasUnsavedChanges = true; });
        
        OnPropertyChanged(nameof(FilteredJourneys));
    }

    private bool CanDeleteJourney() => SelectedJourney != null;

    #endregion

    #region Station CRUD Commands

    [RelayCommand(CanExecute = nameof(CanAddStation))]
    private void AddStation()
    {
        if (SelectedJourney == null) return;

        var station = EntityEditorHelper.AddEntity(
            SelectedJourney.Model.Stations,
            SelectedJourney.Stations,
            () => new Station { Name = "New Station", NumberOfLapsToStop = 2 },
            model => new StationViewModel(model));

        SelectedStation = station;
        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteStation))]
    private void DeleteStation()
    {
        if (SelectedJourney == null) return;

        EntityEditorHelper.DeleteEntity(
            SelectedStation,
            SelectedJourney.Model.Stations,
            SelectedJourney.Stations,
            () => { SelectedStation = null; HasUnsavedChanges = true; });
    }

    private bool CanAddStation() => SelectedJourney != null;
    private bool CanDeleteStation() => SelectedStation != null;

    [RelayCommand(CanExecute = nameof(CanResetJourneyCounter))]
    private void ResetJourneyCounter()
    {
        if (SelectedJourney == null || _journeyManager == null) return;

        var state = _journeyManager.GetState(SelectedJourney.Model.Id);
        if (state != null)
        {
            state.Counter = 0;
            state.CurrentPos = 0;
            state.CurrentStationName = string.Empty;
            state.LastFeedbackTime = null;
            
            // Trigger UI update
            OnPropertyChanged(nameof(SelectedJourney));
        }
    }

    private bool CanResetJourneyCounter() => SelectedJourney != null && _journeyManager != null;

    #endregion

    #region Station Management (City Library)

    [RelayCommand(CanExecute = nameof(CanAddStationToJourney))]
    private void AddStationToJourney()
    {
        if (SelectedCity == null || SelectedJourney == null)
            return;

        // Take the first station from the selected city
        if (SelectedCity.Stations.Count > 0)
        {
            var stationToCopy = SelectedCity.Stations[0];

            // Create a new Station instance (deep copy)
            var newStation = new Station
            {
                Name = stationToCopy.Name,
                Description = stationToCopy.Description,
                NumberOfLapsToStop = 2,
                InPort = stationToCopy.InPort
            };

            // Add to Journey model
            SelectedJourney.Model.Stations.Add(newStation);

            // Add to ViewModel
            var stationVM = new StationViewModel(newStation);
            SelectedJourney.Stations.Add(stationVM);

            HasUnsavedChanges = true;
        }
    }

    private bool CanAddStationToJourney() => true;

    #endregion
}