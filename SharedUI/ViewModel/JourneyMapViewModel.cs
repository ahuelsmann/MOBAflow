// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Interface;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

/// <summary>
/// ViewModel for JourneyMapPage - displays virtual route with station progress.
/// Shows schematic station-to-station visualization with current position indicator.
/// </summary>
public sealed class JourneyMapViewModel : ObservableObject
{
    #region Fields
    // Context
    private readonly IJourneySelectionContext _selectionContext;
    private JourneyViewModel? _observedJourney;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="JourneyMapViewModel"/> class.
    /// </summary>
    /// <param name="selectionContext">Selection context that provides journey and project state.</param>
    public JourneyMapViewModel(IJourneySelectionContext selectionContext)
    {
        ArgumentNullException.ThrowIfNull(selectionContext);
        _selectionContext = selectionContext;

        // Subscribe to journey and project changes
        _selectionContext.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(IJourneySelectionContext.SelectedProject):
                    OnPropertyChanged(nameof(AvailableJourneys));
                    break;
                case nameof(IJourneySelectionContext.SelectedJourney):
                    AttachToSelectedJourney();
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

        AttachToSelectedJourney();
    }

    #region Journey Selection
    /// <summary>
    /// All available journeys from the current project.
    /// </summary>
    public ObservableCollection<JourneyViewModel>? AvailableJourneys =>
        _selectionContext.SelectedProject?.Journeys;

    /// <summary>
    /// Currently selected/active journey.
    /// </summary>
    public JourneyViewModel? SelectedJourney
    {
        get => _selectionContext.SelectedJourney;
        set
        {
            if (_selectionContext.SelectedJourney != value)
            {
                _selectionContext.SelectedJourney = value;
                AttachToSelectedJourney();
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
    public IReadOnlyList<StationViewModel> RouteStations =>
        SelectedJourney?.Stations.Where(station => station.IsRealStation).ToList() ?? [];
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
            var total = RouteStations.Count;
            var currentIndex = SelectedJourney.Stations
                .Take(Math.Clamp(SelectedJourney.CurrentPos + 1, 0, SelectedJourney.Stations.Count))
                .Count(station => station.IsRealStation);
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
            return SelectedJourney == null ? "-" : $"Lap {SelectedJourney.CurrentCounter}";
        }
    }

    /// <summary>
    /// Behavior on last stop description.
    /// </summary>
    public string BehaviorOnLastStopText
    {
        get
        {
            return SelectedJourney == null ? "-" : SelectedJourney.BehaviorOnLastStop.ToString();
        }
    }

    /// <summary>
    /// Journey InPort (sensor address).
    /// </summary>
    public string JourneyInPort
    {
        get
        {
            return SelectedJourney == null ? "-" : SelectedJourney.InPort.ToString();
        }
    }
    #endregion

    private void AttachToSelectedJourney()
    {
        if (_observedJourney != null)
        {
            _observedJourney.PropertyChanged -= OnSelectedJourneyPropertyChanged;
            _observedJourney.Stations.CollectionChanged -= OnSelectedJourneyStationsChanged;
        }

        _observedJourney = _selectionContext.SelectedJourney;
        if (_observedJourney != null)
        {
            _observedJourney.PropertyChanged += OnSelectedJourneyPropertyChanged;
            _observedJourney.Stations.CollectionChanged += OnSelectedJourneyStationsChanged;
        }
    }

    private void OnSelectedJourneyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = sender;
        if (e.PropertyName is nameof(JourneyViewModel.CurrentPos) or nameof(JourneyViewModel.Stations))
        {
            OnPropertyChanged(nameof(RouteStations));
            OnPropertyChanged(nameof(ProgressText));
        }

        if (e.PropertyName == nameof(JourneyViewModel.CurrentCounter))
        {
            OnPropertyChanged(nameof(CounterText));
        }
    }

    private void OnSelectedJourneyStationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        OnPropertyChanged(nameof(RouteStations));
        OnPropertyChanged(nameof(ProgressText));
    }
}