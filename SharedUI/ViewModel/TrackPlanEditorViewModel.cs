// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Domain.TrackPlan;
using Moba.SharedUI.Interface;

using System.Collections.ObjectModel;
using System.IO;

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

        // Load track library
        LoadTrackLibrary();
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
    }

    /// <summary>
    /// Canvas width in pixels.
    /// </summary>
    public double CanvasWidth => 1200;

    /// <summary>
    /// Canvas height in pixels.
    /// </summary>
    public double CanvasHeight => 800;

    /// <summary>
    /// Number of segments with assigned InPorts.
    /// </summary>
    public int AssignedSensorCount => PlacedSegments.Count(s => s.AssignedInPort.HasValue);

    /// <summary>
    /// Status text for sensor assignments.
    /// </summary>
    public string SensorStatusText => $"{AssignedSensorCount} of {PlacedSegments.Count} segments have sensors";
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
        
        System.Diagnostics.Debug.WriteLine($"📡 InPort {inPortValue} assigned to {SelectedSegment.ArticleCode}");
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
        
        System.Diagnostics.Debug.WriteLine($"🗑️ InPort cleared from {SelectedSegment.ArticleCode}");
    }
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
        System.Diagnostics.Debug.WriteLine($"Adding segment: {template.ArticleCode}");
        System.Diagnostics.Debug.WriteLine($"  Type: {template.Type}, Length: {template.Length}mm, Radius: {template.Radius}mm");
        System.Diagnostics.Debug.WriteLine($"  PathData: {pathData}");
        System.Diagnostics.Debug.WriteLine($"  Position: {x}, {y}");

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
        
        System.Diagnostics.Debug.WriteLine($"  Segment added successfully. Total segments: {PlacedSegments.Count}");
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
        var ic = System.Globalization.CultureInfo.InvariantCulture;

        if (template.Type == TrackType.Straight)
        {
            // Horizontal line at position (x, y)
            var length = template.Length * scale;
            return string.Format(ic, "M {0:F2},{1:F2} L {2:F2},{3:F2}", x, y, x + length, y);
        }
        else if (template.Type == TrackType.Curve)
        {
            // Arc path at position (x, y)
            var radius = template.Radius * scale;
            var angleRad = template.Angle * Math.PI / 180.0;
            var endX = x + radius * Math.Sin(angleRad);
            var endY = y + radius * (1 - Math.Cos(angleRad));
            
            // Arc command: A rx,ry rotation large-arc-flag sweep-flag x,y
            return string.Format(ic, "M {0:F2},{1:F2} A {2:F2},{3:F2} 0 0 1 {4:F2},{5:F2}", x, y, radius, radius, endX, endY);
        }
        else if (template.Type is TrackType.TurnoutLeft or TrackType.TurnoutRight)
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
    /// Create a connection between two segments at a snap point.
    /// </summary>
    public void CreateConnection(TrackSegmentViewModel segment1, bool segment1IsStart,
                                  TrackSegmentViewModel segment2, bool segment2IsStart,
                                  double connectionX, double connectionY)
    {
        // Check if connection already exists
        var existingConnection = Connections.FirstOrDefault(c =>
            (c.Segment1Id == segment1.Id && c.Segment2Id == segment2.Id) ||
            (c.Segment1Id == segment2.Id && c.Segment2Id == segment1.Id));

        if (existingConnection != null)
            return;

        var connection = new TrackConnection
        {
            Segment1Id = segment1.Id,
            Segment1IsStart = segment1IsStart,
            Segment2Id = segment2.Id,
            Segment2IsStart = segment2IsStart,
            ConnectionX = connectionX,
            ConnectionY = connectionY
        };

        Connections.Add(connection);

        // Update segment connection states
        segment1.SetConnectionState(segment1IsStart, true, segment2.Id);
        segment2.SetConnectionState(segment2IsStart, true, segment1.Id);

        System.Diagnostics.Debug.WriteLine($"🔗 Connection created: {segment1.ArticleCode} <-> {segment2.ArticleCode}");
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
            var otherIsStart = connection.Segment1Id == segment.Id 
                ? connection.Segment2IsStart 
                : connection.Segment1IsStart;

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

        System.Diagnostics.Debug.WriteLine($"📦 Moved group of {group.Count} segments");
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
            var segment = new TrackSegment
            {
                Id = part.Id,
                Name = $"{articleCode} ({part.Id})",
                ArticleCode = articleCode,
                Type = TrackSegmentType.Straight,
                PathData = pathData,
                CenterX = center.X,
                CenterY = center.Y,
                Rotation = 0,
                Layer = "Default"
            };

            PlacedSegments.Add(new TrackSegmentViewModel(segment));
        }

        OnPropertyChanged(nameof(AssignedSensorCount));
        OnPropertyChanged(nameof(SensorStatusText));

        System.Diagnostics.Debug.WriteLine($"📂 Imported AnyRail layout: {PlacedSegments.Count} segments");
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
    public void ExportToAnyRail(string xmlPath)
    {
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        
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

        System.Diagnostics.Debug.WriteLine($"💾 Exported track plan to: {xmlPath}");
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
}

