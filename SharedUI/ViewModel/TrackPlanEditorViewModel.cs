// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.TrackPlan;
using Interface;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

/// <summary>
/// ViewModel for TrackPlanEditorPage - interactive drag & drop track plan editor.
/// Users can drag track pieces from a toolbar onto a canvas and snap them together.
/// Supports AnyRail XML import/export and InPort assignment for feedback sensors.
/// </summary>
public partial class TrackPlanEditorViewModel : ObservableObject
{
    #region Fields
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IIoService _ioService;
    #endregion

    public TrackPlanEditorViewModel(MainWindowViewModel mainViewModel, IIoService ioService)
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;

        // Subscribe to Solution events for persistence
        _mainViewModel.SolutionSaving += OnSolutionSaving;
        _mainViewModel.SolutionLoaded += OnSolutionLoaded;
        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;

        // Load track library
        LoadTrackLibrary();
    }

    private void OnSolutionSaving(object? sender, EventArgs e)
    {
        SyncToProject();
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
        LoadFromProject();
    }

    private void OnMainViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When SelectedProject changes, load the track layout
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
        {
            LoadFromProject();
        }
    }

    #region Track Library (Toolbar)
    /// <summary>
    /// All available track templates from Piko A-Gleis system.
    /// Displayed in the toolbar for drag & drop.
    /// </summary>
    public ObservableCollection<TrackTemplateViewModel> TrackLibrary { get; } = [];

    /// <summary>
    /// Selected track template in the toolbar (for inspection/info).
    /// </summary>
    [ObservableProperty]
    private TrackTemplateViewModel? selectedTemplate;

    /// <summary>
    /// SVG path data for snap preview line (visual feedback during drag).
    /// </summary>
    [ObservableProperty]
    private string? snapPreviewPathData;

    /// <summary>
    /// Indicates whether snap preview should be visible.
    /// </summary>
    [ObservableProperty]
    private bool isSnapPreviewVisible;

    private void LoadTrackLibrary()
    {
        TrackLibrary.Clear();

        // Group tracks by category for better UX
        var straightTracks = PikoATrackLibrary.GetStraightTracks()
            .Select(t => new TrackTemplateViewModel(t))
            .ToList();

        var curvedTracks = PikoATrackLibrary.GetCurvedTracks()
            .Select(t => new TrackTemplateViewModel(t))
            .ToList();

        var turnouts = PikoATrackLibrary.GetTurnouts()
            .Select(t => new TrackTemplateViewModel(t))
            .ToList();

        var crossings = PikoATrackLibrary.GetCrossings()
            .Select(t => new TrackTemplateViewModel(t))
            .ToList();

        // Add all tracks (maintain category order)
        foreach (var track in straightTracks.Concat(curvedTracks).Concat(turnouts).Concat(crossings))
        {
            TrackLibrary.Add(track);
        }
    }
    #endregion

    #region Canvas (Placed Tracks)
    /// <summary>
    /// All track segments currently placed on the canvas.
    /// </summary>
    public ObservableCollection<TrackSegmentViewModel> PlacedSegments { get; } = [];

    /// <summary>
    /// All connections between track segments.
    /// </summary>
    public List<TrackConnection> Connections { get; } = [];

    /// <summary>
    /// Currently selected segment on the canvas (for editing/deletion).
    /// </summary>
    [ObservableProperty]
    private TrackSegmentViewModel? selectedSegment;

    /// <summary>
    /// Indicates whether a segment is selected (for UI binding).
    /// </summary>
    public bool HasSelectedSegment => SelectedSegment != null;

    /// <summary>
    /// Indicates whether no segment is selected (for placeholder text).
    /// </summary>
    public bool HasNoSelectedSegment => SelectedSegment == null;

    /// <summary>
    /// InPort value being entered for assignment.
    /// </summary>
    [ObservableProperty]
    private double inPortInput = double.NaN;

    partial void OnSelectedSegmentChanged(TrackSegmentViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedSegment));
        OnPropertyChanged(nameof(HasNoSelectedSegment));

        // Update InPort input when selection changes
        if (value != null)
        {
            InPortInput = value.AssignedInPort ?? double.NaN;
        }
        else
        {
            InPortInput = double.NaN;
        }

        // Notify commands about selection change
        DisconnectSelectedSegmentCommand.NotifyCanExecuteChanged();
    }

    #region Canvas Size (configurable work surface)
    /// <summary>
    /// Scale factor: 1 pixel = 0.5mm (so 2400mm = 1200px).
    /// </summary>
    public const double PixelsPerMm = 0.5;

    /// <summary>
    /// Layout name (persisted).
    /// </summary>
    [ObservableProperty]
    private string layoutName = "Untitled Layout";

    /// <summary>
    /// Layout description (persisted).
    /// </summary>
    [ObservableProperty]
    private string? layoutDescription;

    /// <summary>
    /// Track system (e.g., "Piko A-Gleis", "Tillig Elite").
    /// </summary>
    [ObservableProperty]
    private string trackSystem = "Piko A-Gleis";

    /// <summary>
    /// Scale (e.g., "H0", "N", "TT").
    /// </summary>
    [ObservableProperty]
    private string scale = "H0";

    /// <summary>
    /// Canvas width in mm (real-world dimensions).
    /// </summary>
    [ObservableProperty]
    private double canvasWidthMm = 2400;

    /// <summary>
    /// Canvas height in mm (real-world dimensions).
    /// </summary>
    [ObservableProperty]
    private double canvasHeightMm = 1600;

    /// <summary>
    /// Canvas width in pixels (for rendering).
    /// </summary>
    public double CanvasWidth => CanvasWidthMm * PixelsPerMm;

    /// <summary>
    /// Canvas height in pixels (for rendering).
    /// </summary>
    public double CanvasHeight => CanvasHeightMm * PixelsPerMm;

    /// <summary>
    /// Display text for canvas dimensions.
    /// </summary>
    public string CanvasSizeDisplay => $"{CanvasWidthMm:F0} × {CanvasHeightMm:F0} mm";

    partial void OnCanvasWidthMmChanged(double value)
    {
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasSizeDisplay));
    }

    partial void OnCanvasHeightMmChanged(double value)
    {
        OnPropertyChanged(nameof(CanvasHeight));
        OnPropertyChanged(nameof(CanvasSizeDisplay));
    }
    #endregion

    /// <summary>
    /// Number of segments with assigned InPorts.
    /// </summary>
    public int AssignedSensorCount => PlacedSegments.Count(s => s.AssignedInPort.HasValue);

    /// <summary>
    /// Status text for sensor assignments.
    /// </summary>
    public string SensorStatusText => $"{AssignedSensorCount} of {PlacedSegments.Count} segments have sensors";
    #endregion

    #region Selection
    /// <summary>
    /// Select all segments on the canvas (Ctrl+A).
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var segment in PlacedSegments)
        {
            segment.IsSelected = true;
        }
        Debug.WriteLine($"✅ Selected all {PlacedSegments.Count} segments");
    }

    /// <summary>
    /// Deselect all segments.
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var segment in PlacedSegments)
        {
            segment.IsSelected = false;
        }
        SelectedSegment = null;
    }

    /// <summary>
    /// Delete all selected segments.
    /// </summary>
    [RelayCommand]
    private void DeleteSelected()
    {
        var selectedSegments = PlacedSegments.Where(s => s.IsSelected).ToList();
        foreach (var segment in selectedSegments)
        {
            RemoveConnectionsForSegment(segment);
            PlacedSegments.Remove(segment);
        }
        SelectedSegment = null;
        OnPropertyChanged(nameof(AssignedSensorCount));
        OnPropertyChanged(nameof(SensorStatusText));
        Debug.WriteLine($"🗑️ Deleted {selectedSegments.Count} segments");
    }
    #endregion

    #region InPort Assignment
    /// <summary>
    /// Assigns the current InPortInput value to the selected segment.
    /// </summary>
    [RelayCommand]
    private void AssignInPort()
    {
        if (SelectedSegment == null || double.IsNaN(InPortInput) || InPortInput < 1 || InPortInput > 2048)
            return;

        var inPortValue = (uint)InPortInput;

        // Check if InPort is already used by another segment
        var existingSegment = PlacedSegments.FirstOrDefault(s =>
            s.AssignedInPort == inPortValue && s != SelectedSegment);

        if (existingSegment != null)
        {
            // Clear the previous assignment
            existingSegment.AssignedInPort = null;
        }

        SelectedSegment.AssignedInPort = inPortValue;
        OnPropertyChanged(nameof(AssignedSensorCount));
        OnPropertyChanged(nameof(SensorStatusText));
        
        Debug.WriteLine($"📡 InPort {inPortValue} assigned to {SelectedSegment.ArticleCode}");
    }

    /// <summary>
    /// Clears the InPort assignment from the selected segment.
    /// </summary>
    [RelayCommand]
    private void ClearInPort()
    {
        if (SelectedSegment == null)
            return;

        SelectedSegment.AssignedInPort = null;
        InPortInput = double.NaN;
        OnPropertyChanged(nameof(AssignedSensorCount));
        OnPropertyChanged(nameof(SensorStatusText));

        Debug.WriteLine($"🗑️ InPort cleared from {SelectedSegment.ArticleCode}");
    }

    /// <summary>
    /// Disconnect the selected segment from all other segments.
    /// This allows moving the segment independently.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDisconnectSegment))]
    private void DisconnectSelectedSegment()
    {
        if (SelectedSegment == null)
            return;

        RemoveConnectionsForSegment(SelectedSegment);
        Debug.WriteLine($"🔓 Disconnected {SelectedSegment.ArticleCode} from all connections");
    }

    private bool CanDisconnectSegment() =>
        SelectedSegment != null && 
        (SelectedSegment.IsStartConnected || SelectedSegment.IsEndConnected);
    #endregion

    #region Commands
    /// <summary>
    /// Add a track segment to the canvas (from toolbar drag & drop).
    /// </summary>
    [RelayCommand]
    private void AddTrackSegment(TrackTemplateViewModel template)
    {
        // Use default position
        AddTrackSegmentAtPosition(template, 200, 200);
    }

    /// <summary>
    /// Add a track segment at a specific position.
    /// </summary>
    public void AddTrackSegmentAtPosition(TrackTemplateViewModel? template, double x, double y)
    {
        if (template is null)
            return;

        var pathData = GeneratePathData(template, x, y);
        
        // DEBUG: Output to help diagnose visibility issues
        Debug.WriteLine($"Adding segment: {template.ArticleCode}");
        Debug.WriteLine($"  Type: {template.Type}, Length: {template.Length}mm, Radius: {template.Radius}mm");
        Debug.WriteLine($"  PathData: {pathData}");
        Debug.WriteLine($"  Position: {x}, {y}");

        // Create new segment at the specified position
        var segment = new TrackSegment
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"{template.ArticleCode}-{PlacedSegments.Count + 1:D3}",
            Type = (TrackSegmentType)template.Type,
            ArticleCode = template.ArticleCode,
            PathData = pathData,
            CenterX = x,
            CenterY = y,
            Rotation = 0,
            Layer = "Default"
        };

        var viewModel = new TrackSegmentViewModel(segment);
        PlacedSegments.Add(viewModel);
        SelectedSegment = viewModel;
        
        Debug.WriteLine($"  Segment added successfully. Total segments: {PlacedSegments.Count}");
    }

    /// <summary>
    /// Delete the currently selected segment.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteSegment))]
    private void DeleteSelectedSegment()
    {
        if (SelectedSegment is null)
            return;

        PlacedSegments.Remove(SelectedSegment);
        SelectedSegment = null;
    }

    private bool CanDeleteSegment() => SelectedSegment is not null;

    /// <summary>
    /// Clear all segments from the canvas.
    /// </summary>
    [RelayCommand]
    private void ClearCanvas()
    {
        PlacedSegments.Clear();
        SelectedSegment = null;
    }

    /// <summary>
    /// Rotate the selected segment by 15° clockwise.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRotateSegment))]
    private void RotateRight()
    {
        if (SelectedSegment is null)
            return;

        var newRotation = (SelectedSegment.Rotation + 15) % 360;
        SelectedSegment.SetRotation(newRotation);
    }

    /// <summary>
    /// Rotate the selected segment by 15° counter-clockwise.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRotateSegment))]
    private void RotateLeft()
    {
        if (SelectedSegment is null)
            return;

        var newRotation = SelectedSegment.Rotation - 15;
        if (newRotation < 0) newRotation += 360;
        SelectedSegment.SetRotation(newRotation);
    }

    private bool CanRotateSegment() => SelectedSegment is not null;
    #endregion

    #region Helper Methods
    /// <summary>
    /// Generate SVG path data for a track template at specific position.
    /// Uses absolute coordinates so the path renders at the correct canvas position.
    /// Uses InvariantCulture to ensure dot (.) as decimal separator in SVG path data.
    /// </summary>
    private static string GeneratePathData(TrackTemplateViewModel template, double x, double y)
    {
        const double scale = 0.5; // 1mm = 0.5px (smaller for better fit)
        var ic = CultureInfo.InvariantCulture;

        if (template.Type == TrackType.Straight)
        {
            // Horizontal line at position (x, y)
            var length = template.Length * scale;
            return string.Format(ic, "M {0:F2},{1:F2} L {2:F2},{3:F2}", x, y, x + length, y);
        }

        if (template.Type == TrackType.Curve)
        {
            // Arc path at position (x, y)
            var radius = template.Radius * scale;
            var angleRad = template.Angle * Math.PI / 180.0;
            var endX = x + radius * Math.Sin(angleRad);
            var endY = y + radius * (1 - Math.Cos(angleRad));
            
            // Arc command: A rx,ry rotation large-arc-flag sweep-flag x,y
            return string.Format(ic, "M {0:F2},{1:F2} A {2:F2},{3:F2} 0 0 1 {4:F2},{5:F2}", x, y, radius, radius, endX, endY);
        }

        if (template.Type is TrackType.TurnoutLeft or TrackType.TurnoutRight)
        {
            // Simple turnout at position (x, y)
            var length = template.Length * scale;
            var branchY = template.Type == TrackType.TurnoutLeft ? -20 : 20;
            return string.Format(ic, "M {0:F2},{1:F2} L {2:F2},{3:F2} M {4:F2},{5:F2} L {6:F2},{7:F2}", 
                x, y, x + length, y, 
                x + length / 2, y, x + length / 2 + 30, y + branchY);
        }

        // Default: straight line at position
        var defaultLength = template.Length * scale;
        return string.Format(ic, "M {0:F2},{1:F2} L {2:F2},{3:F2}", x, y, x + defaultLength, y);
    }
    #endregion

    #region Connection Management
    /// <summary>
    /// Get the endpoint index of a segment for a given endpoint coordinate.
    /// </summary>
    private static int GetEndpointIndex(TrackSegmentViewModel segment, (double X, double Y) endpoint)
    {
        var endpoints = segment.GetAllEndpoints();
        for (int i = 0; i < endpoints.Count; i++)
        {
            var distance = Math.Sqrt(
                Math.Pow(endpoints[i].X - endpoint.X, 2) +
                Math.Pow(endpoints[i].Y - endpoint.Y, 2));
            
            if (distance < 1) // Within 1px tolerance
                return i;
        }
        
        // Fallback: return the closest endpoint index
        var closest = 0;
        var minDistance = double.MaxValue;
        for (int i = 0; i < endpoints.Count; i++)
        {
            var distance = Math.Sqrt(
                Math.Pow(endpoints[i].X - endpoint.X, 2) +
                Math.Pow(endpoints[i].Y - endpoint.Y, 2));
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = i;
            }
        }
        return closest;
    }

    /// <summary>
    /// Create a connection between two segments at a snap point.
    /// Supports multiple connections per segment (e.g., turnouts with 3-4 endpoints).
    /// </summary>
    public void CreateConnection(TrackSegmentViewModel segment1, bool segment1IsStart,
                                  TrackSegmentViewModel segment2, bool segment2IsStart,
                                  double connectionX, double connectionY)
    {
        var ep = (X: connectionX, Y: connectionY);
        var segment1EndpointIndex = GetEndpointIndex(segment1, ep);
        var segment2EndpointIndex = GetEndpointIndex(segment2, ep);

        // Check if a connection at this specific endpoint combination already exists
        var existingConnection = Connections.FirstOrDefault(c =>
            ((c.Segment1Id == segment1.Id && c.Segment2Id == segment2.Id) ||
             (c.Segment1Id == segment2.Id && c.Segment2Id == segment1.Id)) &&
            ((c.Segment1EndpointIndex == segment1EndpointIndex && c.Segment2EndpointIndex == segment2EndpointIndex) ||
             (c.Segment1EndpointIndex == segment2EndpointIndex && c.Segment2EndpointIndex == segment1EndpointIndex)));

        if (existingConnection != null)
            return;

        var connection = new TrackConnection
        {
            Segment1Id = segment1.Id,
            Segment1EndpointIndex = segment1EndpointIndex,
            Segment2Id = segment2.Id,
            Segment2EndpointIndex = segment2EndpointIndex,
            ConnectionX = connectionX,
            ConnectionY = connectionY
        };

        Connections.Add(connection);

        // Update segment connection states
        segment1.SetConnectionState(segment1IsStart, true, segment2.Id);
        segment2.SetConnectionState(segment2IsStart, true, segment1.Id);

        Debug.WriteLine($"🔗 Connection created: {segment1.ArticleCode}[{segment1EndpointIndex}] <-> {segment2.ArticleCode}[{segment2EndpointIndex}]");
    }

    /// <summary>
    /// Remove all connections for a segment.
    /// </summary>
    public void RemoveConnectionsForSegment(TrackSegmentViewModel segment)
    {
        var connectionsToRemove = Connections
            .Where(c => c.Segment1Id == segment.Id || c.Segment2Id == segment.Id)
            .ToList();

        foreach (var connection in connectionsToRemove)
        {
            // Get the other segment and update its connection state
            var otherSegmentId = connection.Segment1Id == segment.Id 
                ? connection.Segment2Id 
                : connection.Segment1Id;
            var otherEndpointIndex = connection.Segment1Id == segment.Id 
                ? connection.Segment2EndpointIndex 
                : connection.Segment1EndpointIndex;
            var otherIsStart = otherEndpointIndex == 0; // Simple heuristic: index 0 = start

            var otherSegment = PlacedSegments.FirstOrDefault(s => s.Id == otherSegmentId);
            otherSegment?.SetConnectionState(otherIsStart, false, null);

            Connections.Remove(connection);
        }

        // Clear this segment's connection states
        segment.SetConnectionState(true, false, null);
        segment.SetConnectionState(false, false, null);
    }

    /// <summary>
    /// Get all segments connected to a given segment (recursively).
    /// Used for group movement.
    /// </summary>
    public List<TrackSegmentViewModel> GetConnectedGroup(TrackSegmentViewModel startSegment)
    {
        var group = new List<TrackSegmentViewModel>();
        var visited = new HashSet<string>();

        CollectConnectedSegments(startSegment, group, visited);

        return group;
    }

    private void CollectConnectedSegments(TrackSegmentViewModel segment, 
                                           List<TrackSegmentViewModel> group, 
                                           HashSet<string> visited)
    {
        if (visited.Contains(segment.Id))
            return;

        visited.Add(segment.Id);
        group.Add(segment);

        // Find all connections involving this segment
        var connections = Connections.Where(c => 
            c.Segment1Id == segment.Id || c.Segment2Id == segment.Id);

        foreach (var connection in connections)
        {
            var otherSegmentId = connection.Segment1Id == segment.Id 
                ? connection.Segment2Id 
                : connection.Segment1Id;

            var otherSegment = PlacedSegments.FirstOrDefault(s => s.Id == otherSegmentId);
            if (otherSegment != null)
            {
                CollectConnectedSegments(otherSegment, group, visited);
            }
        }
    }

    /// <summary>
    /// Move a group of connected segments by delta.
    /// </summary>
    public void MoveConnectedGroup(TrackSegmentViewModel startSegment, double deltaX, double deltaY)
    {
        var group = GetConnectedGroup(startSegment);

        foreach (var segment in group)
        {
            segment.MoveBy(deltaX, deltaY);
        }

        // Update connection points
        foreach (var connection in Connections.Where(c => 
            group.Any(s => s.Id == c.Segment1Id || s.Id == c.Segment2Id)))
        {
            connection.ConnectionX += deltaX;
            connection.ConnectionY += deltaY;
        }

            Debug.WriteLine($"📦 Moved group of {group.Count} segments");
        }

        /// <summary>
        /// Automatically detect and create connections between segments based on matching endpoints.
        /// Used after AnyRail XML import to restore track connectivity.
        /// Handles turnouts (3-4 endpoints) as well as simple tracks (2 endpoints).
        /// </summary>
        /// <param name="tolerance">Maximum distance (in pixels) for endpoints to be considered connected. Default 5.0 for rounding tolerance.</param>
        public void AutoConnectFromEndpoints(double tolerance = 5.0)
        {
            var segments = PlacedSegments.ToList();
            int connectionsCreated = 0;

            Debug.WriteLine($"🔗 AutoConnect: Checking {segments.Count} segments with tolerance {tolerance}px");

            // Debug: Print first few segment endpoints to verify extraction
            foreach (var seg in segments.Take(3))
            {
                var endpoints = seg.GetAllEndpoints();
                Debug.WriteLine($"   Segment {seg.ArticleCode}: {endpoints.Count} endpoints");
            }

            for (int i = 0; i < segments.Count; i++)
            {
                var seg1 = segments[i];
                var seg1Endpoints = seg1.GetAllEndpoints();

                for (int j = i + 1; j < segments.Count; j++)
                    {
                        var seg2 = segments[j];
                        var seg2Endpoints = seg2.GetAllEndpoints();

                        // Check all combinations of endpoints between the two segments
                        foreach (var ep1 in seg1Endpoints)
                        {
                            foreach (var ep2 in seg2Endpoints)
                            {
                                if (PointsMatch(ep1, ep2, tolerance))
                                {
                                    // Determine if this is start or end for each segment
                                    var seg1IsStart = IsStartEndpoint(seg1, ep1);
                                    var seg2IsStart = IsStartEndpoint(seg2, ep2);

                                    // CreateConnection handles duplicate checks now with endpoint indices
                                    CreateConnection(seg1, seg1IsStart, seg2, seg2IsStart, ep1.X, ep1.Y);
                                    connectionsCreated++;
                                }
                            }
                        }
                    }
                }

                    Debug.WriteLine($"🔗 AutoConnect: Created {connectionsCreated} connections from endpoint matching");
                }

            /// <summary>
            /// Automatically connect a specific segment to nearby segments based on endpoint proximity.
            /// Used when dragging a segment to reconnect it after being disconnected.
            /// </summary>
            /// <param name="segment">The segment to connect.</param>
            /// <param name="tolerance">Maximum distance (in pixels) for endpoints to be considered connected. Default 5.0 for rounding tolerance.</param>
            public void AutoConnectFromEndpoints(TrackSegmentViewModel segment, double tolerance = 5.0)
            {
                var segmentEndpoints = segment.GetAllEndpoints();
                int connectionsCreated = 0;

                Debug.WriteLine($"🔗 AutoConnect for {segment.ArticleCode}: Checking {segmentEndpoints.Count} endpoints");

                foreach (var otherSegment in PlacedSegments.Where(s => s.Id != segment.Id))
                {
                    var otherEndpoints = otherSegment.GetAllEndpoints();

                    foreach (var ep1 in segmentEndpoints)
                    {
                        foreach (var ep2 in otherEndpoints)
                        {
                            if (PointsMatch(ep1, ep2, tolerance))
                            {
                                var segIsStart = IsStartEndpoint(segment, ep1);
                                var otherIsStart = IsStartEndpoint(otherSegment, ep2);

                                // CreateConnection handles duplicate checks now with endpoint indices
                                CreateConnection(segment, segIsStart, otherSegment, otherIsStart, ep1.X, ep1.Y);
                                connectionsCreated++;
                            }
                        }
                    }
                }

                if (connectionsCreated > 0)
                {
                    Debug.WriteLine($"🔗 AutoConnect: Created {connectionsCreated} connections for {segment.ArticleCode}");
                }
            }

            /// <summary>
            /// Determine if an endpoint is closer to the segment's start point.
            /// </summary>
        private static bool IsStartEndpoint(TrackSegmentViewModel segment, (double X, double Y) endpoint)
        {
            var startDist = Math.Pow(segment.StartPointX - endpoint.X, 2) + Math.Pow(segment.StartPointY - endpoint.Y, 2);
            var endDist = Math.Pow(segment.EndPointX - endpoint.X, 2) + Math.Pow(segment.EndPointY - endpoint.Y, 2);
            return startDist <= endDist;
        }

        /// <summary>
        /// Check if two points are within tolerance distance of each other.
        /// </summary>
        private static bool PointsMatch((double X, double Y) p1, (double X, double Y) p2, double tolerance)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return dx * dx + dy * dy <= tolerance * tolerance;
        }
        #endregion

        #region AnyRail Import/Export
    /// <summary>
    /// Current loaded AnyRail layout (if imported).
    /// </summary>
    private AnyRailLayout? _anyRailLayout;

    /// <summary>
    /// Browse and import an AnyRail XML layout file.
    /// </summary>
    [RelayCommand]
    private async Task BrowseAndLoadAnyRailLayoutAsync()
    {
        var file = await _ioService.BrowseForXmlFileAsync();
        if (file == null)
            return;

        LoadAnyRailLayout(file);
    }

    /// <summary>
    /// Load an AnyRail XML layout and convert to PlacedSegments.
    /// </summary>
    public void LoadAnyRailLayout(string xmlPath)
    {
        if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
            return;

        _anyRailLayout = AnyRailLayout.Parse(xmlPath);

        // Clear existing segments
        PlacedSegments.Clear();
        Connections.Clear();

        // Convert AnyRail parts to TrackSegments
        foreach (var part in _anyRailLayout.Parts)
        {
            var pathData = part.ToPathData();
            if (string.IsNullOrWhiteSpace(pathData))
                continue;

            var center = part.GetCenter();
            var articleCode = part.GetArticleCode();
            var trackType = part.GetTrackSegmentType();
            
            var segment = new TrackSegment
            {
                Id = part.Id,
                Name = $"{articleCode} ({part.Id})",
                ArticleCode = articleCode,
                Type = trackType,
                PathData = pathData,
                CenterX = center.X,
                CenterY = center.Y,
                Rotation = 0,
                Layer = "Default"
            };

                PlacedSegments.Add(new TrackSegmentViewModel(segment));
            }

            // Automatically detect and create connections based on matching endpoints
            AutoConnectFromEndpoints();

            OnPropertyChanged(nameof(AssignedSensorCount));
            OnPropertyChanged(nameof(SensorStatusText));

            Debug.WriteLine($"📂 Imported AnyRail layout: {PlacedSegments.Count} segments");
    }

    /// <summary>
    /// Export current track plan to AnyRail XML format.
    /// </summary>
    [RelayCommand]
    private async Task ExportToAnyRailAsync()
    {
        if (PlacedSegments.Count == 0)
            return;

        var file = await _ioService.SaveXmlFileAsync("trackplan.xml");
        if (file == null)
            return;

        ExportToAnyRail(file);
    }

    /// <summary>
    /// Export track plan to AnyRail XML format.
    /// </summary>
    public void ExportToAnyRail(String xmlPath)
    {
        var ic = CultureInfo.InvariantCulture;
        
        using var writer = new StreamWriter(xmlPath);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        writer.WriteLine($"<layout width=\"{CanvasWidth.ToString(ic)}\" height=\"{CanvasHeight.ToString(ic)}\" scaleX=\"1\" scaleY=\"1\">");
        writer.WriteLine("  <parts>");

        foreach (var segment in PlacedSegments)
        {
            writer.WriteLine($"    <part id=\"{segment.Id}\" type=\"{GetAnyRailType(segment)}\">");
            writer.WriteLine("      <drawing>");
            
            // Write path as line (simplified - full arc support would need more work)
            var startX = segment.StartPointX.ToString(ic);
            var startY = segment.StartPointY.ToString(ic);
            var endX = segment.EndPointX.ToString(ic);
            var endY = segment.EndPointY.ToString(ic);
            writer.WriteLine($"        <line pt1=\"{startX},{startY}\" pt2=\"{endX},{endY}\" />");
            
            writer.WriteLine("      </drawing>");
            
            // Write InPort if assigned
            if (segment.AssignedInPort.HasValue)
            {
                writer.WriteLine($"      <inport>{segment.AssignedInPort.Value}</inport>");
            }
            
            writer.WriteLine("    </part>");
        }

        writer.WriteLine("  </parts>");
        writer.WriteLine("</layout>");

        Debug.WriteLine($"💾 Exported track plan to: {xmlPath}");
    }

        private static string GetAnyRailType(TrackSegmentViewModel segment)
        {
            return segment.ArticleCode switch
            {
                var s when s.StartsWith("G") => "Straight",
                var s when s.StartsWith("R") => "Curve",
                "WL" => "LeftRegularTurnout",
                "WR" => "RightRegularTurnout",
                "DKW" => "DoubleSlip",
                "DWW" => "ThreeWay",
                _ => "Straight"
            };
        }
        #endregion

        #region Project Persistence
        /// <summary>
        /// Sync current track layout to the selected Project for JSON persistence.
        /// Call this before saving the Solution.
        /// </summary>
        public void SyncToProject()
        {
            var project = _mainViewModel.SelectedProject?.Model;
            if (project == null)
            {
                Debug.WriteLine("⚠️ SyncToProject: No project selected");
                return;
            }

            // Create or update TrackLayout
            project.TrackLayout ??= new TrackLayout();

            // Sync layout properties
            project.TrackLayout.Name = LayoutName;
            project.TrackLayout.Description = LayoutDescription;
            project.TrackLayout.TrackSystem = TrackSystem;
            project.TrackLayout.Scale = Scale;
            project.TrackLayout.WidthMm = CanvasWidthMm;
            project.TrackLayout.HeightMm = CanvasHeightMm;

            // Convert ViewModels to Domain models
            project.TrackLayout.Segments.Clear();
            foreach (var vm in PlacedSegments)
            {
                project.TrackLayout.Segments.Add(vm.Model);
            }

            // Copy connections
            project.TrackLayout.Connections.Clear();
            project.TrackLayout.Connections.AddRange(Connections);

            Debug.WriteLine($"💾 SyncToProject: {project.TrackLayout.Segments.Count} segments, {project.TrackLayout.Connections.Count} connections");
        }

        /// <summary>
        /// Load track layout from the selected Project.
        /// Call this when a Project is selected or Solution is loaded.
        /// </summary>
        public void LoadFromProject()
        {
            var project = _mainViewModel.SelectedProject?.Model;
            if (project?.TrackLayout == null)
            {
                Debug.WriteLine("📂 LoadFromProject: No track layout in project");
                return;
            }

            var layout = project.TrackLayout;

            // Clear existing data
            PlacedSegments.Clear();
            Connections.Clear();

            // Load layout properties
            LayoutName = layout.Name;
            LayoutDescription = layout.Description;
            TrackSystem = layout.TrackSystem;
            Scale = layout.Scale;
            CanvasWidthMm = layout.WidthMm;
            CanvasHeightMm = layout.HeightMm;

            // Load segments
            foreach (var segment in layout.Segments)
            {
                PlacedSegments.Add(new TrackSegmentViewModel(segment));
            }

            // Load connections
            Connections.AddRange(layout.Connections);

            // Restore connection states on ViewModels
            foreach (var connection in Connections)
            {
                var seg1 = PlacedSegments.FirstOrDefault(s => s.Id == connection.Segment1Id);
                var seg2 = PlacedSegments.FirstOrDefault(s => s.Id == connection.Segment2Id);

                var seg1IsStart = connection.Segment1EndpointIndex == 0; // Index 0 = start
                var seg2IsStart = connection.Segment2EndpointIndex == 0; // Index 0 = start
                seg1?.SetConnectionState(seg1IsStart, true, connection.Segment2Id);
                seg2?.SetConnectionState(seg2IsStart, true, connection.Segment1Id);
            }

            OnPropertyChanged(nameof(AssignedSensorCount));
            OnPropertyChanged(nameof(SensorStatusText));

            Debug.WriteLine($"📂 LoadFromProject: {PlacedSegments.Count} segments, {Connections.Count} connections");
        }
        #endregion
    }

