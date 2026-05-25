// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using Backend.Interface;

using Domain;
using Domain.Enum;

using Helper;

/// <summary>
/// MainWindowViewModel - Journey and Station Management
/// Handles Journey and Station CRUD operations.
/// </summary>
public partial class MainWindowViewModel
{
    #region Journey Factory
    /// <summary>
    /// Creates a JourneyViewModel with SessionState.
    /// Runtime state is projected separately via <see cref="IMobaRuntime"/> snapshots.
    /// </summary>
    private JourneyViewModel CreateJourneyViewModel(Journey journey)
    {
        return new JourneyViewModel(
            journey,
            SelectedProject?.Model ?? new Project(),
            _uiDispatcher);
    }
    #endregion

    #region Journey Search/Filter

    /// <summary>
    /// Controls whether the City Library panel is visible on JourneysPage.
    /// </summary>
    private bool _isCityLibraryVisible = true;

    public bool IsCityLibraryVisible
    {
        get => _isCityLibraryVisible;
        set
        {
            if (SetProperty(ref _isCityLibraryVisible, value))
                PersistLayoutState(layout => layout.JourneysPage.IsCityLibraryExpanded = value);
        }
    }

    /// <summary>
    /// Controls whether the Workflow Library panel is visible on JourneysPage.
    /// </summary>
    private bool _isWorkflowLibraryVisible = true;

    public bool IsWorkflowLibraryVisible
    {
        get => _isWorkflowLibraryVisible;
        set
        {
            if (SetProperty(ref _isWorkflowLibraryVisible, value))
                PersistLayoutState(layout => layout.JourneysPage.IsWorkflowLibraryExpanded = value);
        }
    }

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
        ObserveBackgroundTask(_mobaRuntime.ActivateProjectAsync(SelectedProject.Model), "Activate project runtime");
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
        ObserveBackgroundTask(_mobaRuntime.ActivateProjectAsync(SelectedProject.Model), "Activate project runtime");
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
        AddStationToSelectedJourney(CreateStation("New Station", DefaultStationLapsToStop));
    }

    [RelayCommand(CanExecute = nameof(CanAddStation))]
    private void AddEvent()
    {
        AddEventToSelectedJourney();
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
    private async Task ResetJourneyCounter()
    {
        if (SelectedJourney == null) return;

        SelectedJourney.ResetCommand.Execute(null);
        await _mobaRuntime.ResetJourneyAsync(SelectedJourney.Model.Id).ConfigureAwait(false);
    }

    private bool CanResetJourneyCounter() => SelectedJourney != null;
    #endregion

    #region Station Management (City Library)
    [RelayCommand(CanExecute = nameof(CanAddStationToJourney))]
    private void AddStationToJourney()
    {
        if (SelectedCity == null || SelectedJourney == null)
            return;

        if (SelectedCity.IsVirtual)
        {
            AddEventToSelectedJourney();
            return;
        }

        // Take the first station from the selected city (name only!)
        var cityStation = SelectedCity.Stations.FirstOrDefault();
        if (cityStation != null)
        {
            // Create NEW Station (copy name from City Library)
            AddStationToSelectedJourney(CreateStation(cityStation.Name, DefaultStationLapsToStop, false));
        }
    }

    private bool CanAddStationToJourney() => true;
    #endregion

    private Station CreateStation(string name, uint lapsToStop, bool isVirtual = false)
    {
        return new Station
        {
            Name = name,
            InPort = 0,
            NumberOfLapsToStop = lapsToStop,
            IsExitOnLeft = false,
            IsVirtual = isVirtual
        };
    }

    private void AddEventToSelectedJourney()
    {
        if (SelectedJourney == null)
        {
            return;
        }

        AddStationToSelectedJourney(CreateStation(GetNextEventName(), DefaultEventLapsToStop, true));
    }

    private string GetNextEventName()
    {
        if (SelectedJourney == null)
        {
            return "Event1";
        }

        var nextNumber = 1;
        while (SelectedJourney.Model.Stations.Any(station => station.IsVirtual && station.Name.Equals($"Event{nextNumber}", StringComparison.OrdinalIgnoreCase)))
        {
            nextNumber++;
        }

        return $"Event{nextNumber}";
    }

    private void AddStationToSelectedJourney(Station station)
    {
        if (SelectedJourney == null)
        {
            return;
        }

        SelectedJourney.Model.Stations.Add(station);
        SelectedJourney.RefreshStations();
        SelectedStation = SelectedJourney.Stations.LastOrDefault();
    }
}
