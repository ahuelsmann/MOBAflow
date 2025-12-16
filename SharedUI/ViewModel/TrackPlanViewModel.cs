// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Domain.TrackPlan;
using Moba.SharedUI.Interface;

using System.Collections.ObjectModel;
using System.IO;

/// <summary>
/// ViewModel for TrackPlanPage - displays physical track layout with clickable segments.
/// Each segment can be selected and assigned an InPort for feedback sensors.
/// </summary>
public partial class TrackPlanViewModel : ObservableObject
{
    #region Fields
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IIoService _ioService;
    private TrackLayout? _layout;
    private AnyRailLayout? _anyRailLayout;
    #endregion

    public TrackPlanViewModel(MainWindowViewModel mainViewModel, IIoService ioService)
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;

        // Subscribe to journey and project state changes
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
                    OnPropertyChanged(nameof(CurrentJourneyName));
                    OnPropertyChanged(nameof(CurrentStationName));
                    OnPropertyChanged(nameof(CurrentLapDisplay));
                    break;
            }
        };

        // No default layout - user must import via "Import AnyRail" button
    }

    #region Track Segments

    /// <summary>
    /// All track segments in the layout.
    /// </summary>
    public ObservableCollection<TrackSegmentViewModel> Segments { get; } = [];

    /// <summary>
    /// Currently selected segment (for InPort assignment).
    /// </summary>
    [ObservableProperty]
    private TrackSegmentViewModel? selectedSegment;

    /// <summary>
    /// InPort value being entered for assignment.
    /// NumberBox uses double, so we use double here and convert to uint when assigning.
    /// </summary>
    [ObservableProperty]
    private double inPortInput;

    /// <summary>
    /// Indicates whether a track plan is loaded.
    /// </summary>
    public bool HasTrackPlan => Segments.Count > 0;

    /// <summary>
    /// Canvas width for the layout.
    /// </summary>
    public double CanvasWidth => _layout?.CanvasWidth ?? 1000;

    /// <summary>
    /// Canvas height for the layout.
    /// </summary>
    public double CanvasHeight => _layout?.CanvasHeight ?? 600;

    /// <summary>
    /// Name of the current layout.
    /// </summary>
    public string LayoutName => _layout?.Name ?? "No layout loaded";

    /// <summary>
    /// Track system description.
    /// </summary>
    public string TrackSystemInfo => _layout != null
        ? $"{_layout.TrackSystem} ({_layout.Scale})"
        : string.Empty;

    /// <summary>
    /// Number of segments with assigned InPorts.
    /// </summary>
    public int AssignedSensorCount => Segments.Count(s => s.HasInPort);

    /// <summary>
    /// Status text showing sensor assignment info.
    /// </summary>
    public string SensorStatusText => $"{AssignedSensorCount} sensors assigned";

    #endregion

    #region Journey Map

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

    /// <summary>
    /// Stations of the selected journey for route display.
    /// </summary>
    public ObservableCollection<StationViewModel>? RouteStations =>
        SelectedJourney?.Stations;

    #endregion

    #region Journey Status

    /// <summary>
    /// Current journey name for status bar.
    /// </summary>
    public string CurrentJourneyName => _mainViewModel.SelectedJourney?.Name ?? "(No journey active)";

    /// <summary>
    /// Current station name for status bar.
    /// </summary>
    public string CurrentStationName => _mainViewModel.SelectedJourney?.CurrentStation ?? "(No station)";

    /// <summary>
    /// Current lap display (e.g., "2/6").
    /// </summary>
    public string CurrentLapDisplay
    {
        get
        {
            var journey = _mainViewModel.SelectedJourney;
            if (journey == null)
            {
                return "-";
            }

            return $"{journey.CurrentCounter}/{journey.Stations.Count}";
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Selects a track segment for InPort assignment.
    /// </summary>
    [RelayCommand]
    private void SelectSegment(TrackSegmentViewModel? segment)
    {
        // Deselect previous
        if (SelectedSegment != null)
        {
            SelectedSegment.IsSelected = false;
        }

        SelectedSegment = segment;

        // Select new
        if (segment != null)
        {
            segment.IsSelected = true;
            InPortInput = segment.AssignedInPort ?? double.NaN;
        }
        else
        {
            InPortInput = double.NaN;
        }
    }

    /// <summary>
    /// Assigns the current InPortInput value to the selected segment.
    /// </summary>
    [RelayCommand]
    private void AssignInPort()
    {
        if (SelectedSegment == null || double.IsNaN(InPortInput) || InPortInput < 1 || InPortInput > 2048)
        {
            return;
        }

        var inPortValue = (uint)InPortInput;

        // Check if InPort is already used by another segment
        var existingSegment = Segments.FirstOrDefault(s =>
            s.AssignedInPort == inPortValue && s != SelectedSegment);

        if (existingSegment != null)
        {
            // Clear the previous assignment
            existingSegment.AssignedInPort = null;
        }

        SelectedSegment.AssignedInPort = inPortValue;
        OnPropertyChanged(nameof(AssignedSensorCount));
        OnPropertyChanged(nameof(SensorStatusText));
    }

    /// <summary>
    /// Clears the InPort assignment from the selected segment.
    /// </summary>
    [RelayCommand]
    private void ClearInPort()
    {
        if (SelectedSegment == null)
        {
            return;
        }

        SelectedSegment.AssignedInPort = null;
        InPortInput = double.NaN;
        OnPropertyChanged(nameof(AssignedSensorCount));
        OnPropertyChanged(nameof(SensorStatusText));
    }


    /// <summary>
    /// Deselects the current segment.
    /// </summary>
    [RelayCommand]
    private void DeselectSegment()
    {
        SelectSegment(null);
    }

    /// <summary>
    /// Opens a file picker and loads an AnyRail XML layout file.
    /// </summary>
    [RelayCommand]
    private async Task BrowseAndLoadAnyRailLayoutAsync()
    {
        var xmlPath = await _ioService.BrowseForXmlFileAsync();
        if (!string.IsNullOrWhiteSpace(xmlPath))
        {
            LoadAnyRailLayout(xmlPath);
        }
    }

    #endregion

    #region AnyRailLayout Support

    /// <summary>
    /// Das aktuell geladene AnyRail-Layout.
    /// </summary>
    public AnyRailLayout? AnyRailLayout
    {
        get => _anyRailLayout;
        private set => SetProperty(ref _anyRailLayout, value);
    }

    /// <summary>
    /// Gibt an, ob ein AnyRail-Layout geladen ist.
    /// </summary>
    public bool HasAnyRailLayout => AnyRailLayout != null;

    /// <summary>
    /// Breite des AnyRail-Layouts (falls geladen).
    /// </summary>
    public double AnyRailWidth => AnyRailLayout?.Width ?? 0;

    /// <summary>
    /// Höhe des AnyRail-Layouts (falls geladen).
    /// </summary>
    public double AnyRailHeight => AnyRailLayout?.Height ?? 0;

    /// <summary>
    /// Teile (Parts) des AnyRail-Layouts (falls geladen).
    /// </summary>
    public IReadOnlyList<AnyRailPart> AnyRailParts => AnyRailLayout?.Parts ?? (IReadOnlyList<AnyRailPart>)[];

    /// <summary>
    /// Lädt ein AnyRail-Layout aus einer XML-Datei und konvertiert es zu TrackSegments.
    /// </summary>
    [RelayCommand]
    private void LoadAnyRailLayout(string xmlPath)
    {
        if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
            return;

        AnyRailLayout = AnyRailLayout.Parse(xmlPath);

        // Konvertiere AnyRail-Parts zu TrackSegments für die Anzeige
        Segments.Clear();
        var index = 0;
        foreach (var part in AnyRailLayout.Parts)
        {
            var pathData = part.ToPathData();
            if (string.IsNullOrWhiteSpace(pathData))
                continue;

            var center = part.GetCenter();
            var articleCode = part.GetArticleCode();
            var segment = new TrackSegment
            {
                Id = part.Id,
                Name = $"{articleCode} ({part.Id})",
                ArticleCode = articleCode,
                Type = TrackSegmentType.Straight, // Default, könnte später verfeinert werden
                PathData = pathData,
                CenterX = center.X,
                CenterY = center.Y,
                Layer = "AnyRail"
            };
            Segments.Add(new TrackSegmentViewModel(segment));
            index++;
        }


        // Aktualisiere Canvas-Dimensionen aus AnyRail-Layout
        // Verwende die Original-Dimensionen (nicht skaliert), da die Koordinaten ebenfalls unskaliert sind
        // Die Viewbox in XAML übernimmt die Skalierung auf die verfügbare Fläche
        _layout = new TrackLayout
        {
            Name = "AnyRail Import",
            CanvasWidth = AnyRailLayout.Width,
            CanvasHeight = AnyRailLayout.Height
        };

        OnPropertyChanged(nameof(HasAnyRailLayout));
        OnPropertyChanged(nameof(AnyRailWidth));
        OnPropertyChanged(nameof(AnyRailHeight));
        OnPropertyChanged(nameof(AnyRailParts));
        OnPropertyChanged(nameof(HasTrackPlan));
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
        OnPropertyChanged(nameof(LayoutName));
        OnPropertyChanged(nameof(TrackSystemInfo));
        OnPropertyChanged(nameof(AssignedSensorCount));
        OnPropertyChanged(nameof(SensorStatusText));

        System.Diagnostics.Debug.WriteLine($"AnyRail Import: {Segments.Count} segments, Canvas: {CanvasWidth}x{CanvasHeight}");
    }
    #endregion

    #region Private Methods

    /// <summary>
    /// Updates all segments based on feedback from Z21.
    /// </summary>
    /// <param name="inPort">The InPort that received feedback.</param>
    /// <param name="isOccupied">Whether the track section is occupied.</param>
    public void UpdateFeedback(uint inPort, bool isOccupied)
    {
        foreach (var segment in Segments)
        {
            segment.UpdateFeedback(inPort, isOccupied);
        }
    }

    #endregion

    partial void OnSelectedSegmentChanged(TrackSegmentViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedSegment));
    }

    /// <summary>
    /// Indicates whether a segment is currently selected.
    /// </summary>
    public bool HasSelectedSegment => SelectedSegment != null;
}
