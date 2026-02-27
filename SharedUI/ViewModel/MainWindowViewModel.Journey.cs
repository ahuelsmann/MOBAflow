// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Service;

using CommunityToolkit.Mvvm.Input;

using Domain;
using Domain.Enum;

using Helper;

using Microsoft.Extensions.Logging;

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
        if (_journeyManager == null || SelectedProject == null)
        {
            // Fallback: Create simple ViewModel without SessionState
            return new JourneyViewModel(
                journey,
                SelectedProject?.Model ?? new Project(),
                _uiDispatcher);
        }

        var state = _journeyManager.GetState(journey.Id);

        // If state doesn't exist yet (journey just created), create dummy state
        return state == null || SelectedProject == null
            ? new JourneyViewModel(
                journey,
                SelectedProject?.Model ?? new Project(),
                _uiDispatcher)
            : new JourneyViewModel(
            journey,
            SelectedProject.Model,
            state,
            _journeyManager,
            _uiDispatcher);
    }
    #endregion

    #region Journey Search/Filter

    /// <summary>
    /// Controls whether the City Library panel is visible on JourneysPage.
    /// </summary>
    public bool IsCityLibraryVisible
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    /// <summary>
    /// Controls whether the Workflow Library panel is visible on JourneysPage.
    /// </summary>
    public bool IsWorkflowLibraryVisible
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    /// <summary>
    /// Gets or sets the search text used to filter journeys by name on the Journeys page.
    /// </summary>
    public string JourneySearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(FilteredJourneys));
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets the filtered journeys based on search text.
    /// Returns all journeys if search is empty.
    /// </summary>
    public List<JourneyViewModel> FilteredJourneys
    {
        get
        {
            if (SelectedProject == null)
                return [];

            var journeys = SelectedProject.Journeys;

            return string.IsNullOrWhiteSpace(JourneySearchText)
                ? [.. journeys]
                : [.. journeys.Where(j => j.Name.Contains(JourneySearchText, StringComparison.OrdinalIgnoreCase))];
        }
    }
    #endregion

    #region Journey CRUD Commands
    [RelayCommand]
    private void AddJourney()
    {
        if (SelectedProject == null) return;

        var journey = EntityEditorHelper.AddEntity(
            SelectedProject.Model.Journeys,
            SelectedProject.Journeys,
            () => new Journey { Name = "New Journey", BehaviorOnLastStop = BehaviorOnLastStop.None },
            model => CreateJourneyViewModel(model));

        SelectedJourney = journey;
        OnPropertyChanged(nameof(FilteredJourneys));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteJourney))]
    private void DeleteJourney()
    {
        if (SelectedProject == null) return;

        EntityEditorHelper.DeleteEntity(
            SelectedJourney,
            SelectedProject.Model.Journeys,
            SelectedProject.Journeys,
            () => SelectedJourney = null);

        OnPropertyChanged(nameof(FilteredJourneys));
    }

    private bool CanDeleteJourney() => SelectedJourney != null;
    #endregion

    #region Station CRUD Commands
    [RelayCommand(CanExecute = nameof(CanAddStation))]
    private void AddStation()
    {
        if (SelectedJourney == null || SelectedProject == null) return;

        // Note: This creates a placeholder station.
        // In practice, stations should be added from City Library via drag & drop.
        var newStation = new Station
        {
            Name = "New Station",
            NumberOfLapsToStop = 2,
            IsExitOnLeft = false
        };

        // Add Station directly to Journey
        SelectedJourney.Model.Stations.Add(newStation);

        // Refresh Journey's Stations collection
        SelectedJourney.RefreshStations();

        // Select the new station
        var stationVm = SelectedJourney.Stations.LastOrDefault();
        if (stationVm != null)
        {
            SelectedStation = stationVm;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteStation))]
    private void DeleteStation()
    {
        if (SelectedJourney == null || SelectedStation == null) return;

        // Find and remove Station by Id
        var station = SelectedJourney.Model.Stations
            .FirstOrDefault(s => s.Id == SelectedStation.Model.Id);

        if (station != null)
        {
            SelectedJourney.Model.Stations.Remove(station);
        }

        // Refresh Journey's Stations collection
        SelectedJourney.RefreshStations();

        SelectedStation = null;
    }

    private bool CanAddStation() => SelectedJourney != null;
    private bool CanDeleteStation() => SelectedStation != null;

    [RelayCommand(CanExecute = nameof(CanResetJourneyCounter))]
    private void ResetJourneyCounter()
    {
        if (SelectedJourney == null) return;

        // Use JourneyManager.Reset() to properly reset state (respects FirstPos)
        if (_journeyManager != null)
        {
            _journeyManager.Reset(SelectedJourney.Model);

            // Get the updated state and sync to ViewModel
            var state = _journeyManager.GetState(SelectedJourney.Model.Id);
            if (state != null)
            {
                SelectedJourney.UpdateFromSessionState(state);
            }
        }
        else
        {
            // Fallback when JourneyManager not available (e.g., tests)
            SelectedJourney.ResetCommand.Execute(null);
        }
    }

    private bool CanResetJourneyCounter() => SelectedJourney != null;
    #endregion

    #region Station Management (City Library)
    [RelayCommand(CanExecute = nameof(CanAddStationToJourney))]
    private void AddStationToJourney()
    {
        if (SelectedCity == null || SelectedJourney == null)
            return;

        // Take the first station from the selected city (name only!)
        var cityStation = SelectedCity.Stations.FirstOrDefault();
        if (cityStation != null)
        {

            // Create NEW Station (copy name from City Library)
            var newStation = new Station
            {
                Name = cityStation.Name,
                InPort = 1,  // User must configure!
                NumberOfLapsToStop = 2,
                IsExitOnLeft = false
            };

            // Add Station directly to Journey
            SelectedJourney.Model.Stations.Add(newStation);

            // Refresh Journey's Stations collection
            SelectedJourney.RefreshStations();
        }
    }

    private bool CanAddStationToJourney() => true;
    #endregion

    #region Action Execution Error Handling
    /// <summary>
    /// Handles action execution errors from WorkflowService.
    /// Displays error message to user and logs to application log (MonitorPage).
    /// </summary>
    private void OnActionExecutionError(object? sender, ActionExecutionErrorEventArgs e)
    {
        // Dispatch to UI thread for UI updates
        _uiDispatcher.InvokeOnUi(() =>
        {
            // Set status text for immediate visibility
            StatusText = $"❌ Action '{e.Action.Name}' failed: {e.ErrorMessage}";

            // Log to application log (visible in MonitorPage)
            _logger.LogError(e.Exception, "Action '{ActionName}' execution failed: {ErrorMessage}",
                e.Action.Name, e.ErrorMessage);
        });
    }
    #endregion
}
