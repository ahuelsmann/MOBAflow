// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for JourneyMapPage - displays virtual route with station progress.
/// Shows schematic station-to-station visualization with current position indicator.
/// </summary>
public partial class JourneyMapViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;

    public JourneyMapViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        // Subscribe to journey and project changes
        _mainViewModel.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowViewModel.SelectedProject):
                    OnPropertyChanged(nameof(AvailableJourneys));
                    break;
                case nameof(MainWindowViewModel.SelectedJourney):
                    OnPropertyChanged(nameof(SelectedJourney));
                    OnPropertyChanged(nameof(HasSelectedJourney));
                    OnPropertyChanged(nameof(RouteStations));
                    OnPropertyChanged(nameof(ProgressText));
                    OnPropertyChanged(nameof(CounterText));
                    OnPropertyChanged(nameof(BehaviorOnLastStopText));
                    OnPropertyChanged(nameof(JourneyInPort));
                    break;
            }
        };
    }

    #region Journey Selection
    /// <summary>
    /// All available journeys from the current project.
    /// </summary>
    public ObservableCollection<JourneyViewModel>? AvailableJourneys =>
        _mainViewModel.SelectedProject?.Journeys;

    /// <summary>
    /// Currently selected/active journey.
    /// </summary>
    public JourneyViewModel? SelectedJourney
    {
        get => _mainViewModel.SelectedJourney;
        set
        {
            if (_mainViewModel.SelectedJourney != value)
            {
                _mainViewModel.SelectedJourney = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedJourney));
                OnPropertyChanged(nameof(RouteStations));
            }
        }
    }

    /// <summary>
    /// Indicates whether a journey is selected.
    /// </summary>
    public bool HasSelectedJourney => SelectedJourney != null;
    #endregion

    #region Route Visualization
    /// <summary>
    /// Stations of the selected journey for route display.
    /// </summary>
    public ObservableCollection<StationViewModel>? RouteStations =>
        SelectedJourney?.Stations;
    #endregion

    #region Status Bar Properties
    /// <summary>
    /// Progress text (e.g., "Station 2 of 6").
    /// </summary>
    public string ProgressText
    {
        get
        {
            if (SelectedJourney == null) return "-";
            var currentIndex = SelectedJourney.CurrentPos + 1;
            var total = SelectedJourney.Stations.Count;
            return $"Station {currentIndex} of {total}";
        }
    }

    /// <summary>
    /// Counter text (e.g., "Lap 1/2").
    /// </summary>
    public string CounterText
    {
        get
        {
            if (SelectedJourney == null) return "-";
            return $"Lap {SelectedJourney.CurrentCounter}";
        }
    }

    /// <summary>
    /// Behavior on last stop description.
    /// </summary>
    public string BehaviorOnLastStopText
    {
        get
        {
            if (SelectedJourney == null) return "-";
            return SelectedJourney.BehaviorOnLastStop.ToString();
        }
    }

    /// <summary>
    /// Journey InPort (sensor address).
    /// </summary>
    public string JourneyInPort
    {
        get
        {
            if (SelectedJourney == null) return "-";
            return SelectedJourney.InPort.ToString();
        }
    }
    #endregion
}
