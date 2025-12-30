// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.TrackPlan;
using Interface;
using Service;
using Renderer;
using System.Collections.ObjectModel;
using System.Diagnostics;

/// <summary>
/// ViewModel for TrackPlanEditorPage - topology-based track plan editor.
/// Positions and paths are calculated by TopologyRenderer from the connection graph.
/// </summary>
public partial class TrackPlanEditorViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IIoService _ioService;
    private readonly TopologyRenderer _renderer;

    public TrackPlanEditorViewModel(MainWindowViewModel mainViewModel, IIoService ioService)
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;
        _renderer = new TopologyRenderer(new TrackGeometryLibrary());

        _mainViewModel.SolutionSaving += (_, _) => SyncToProject();
        _mainViewModel.SolutionLoaded += (_, _) => LoadFromProject();
        _mainViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
                LoadFromProject();
        };

        LoadTrackLibrary();
    }

    #region Track Library
    public ObservableCollection<TrackTemplateViewModel> TrackLibrary { get; } = [];

    [ObservableProperty]
    private TrackTemplateViewModel? selectedTemplate;

    private void LoadTrackLibrary()
    {
        TrackLibrary.Clear();
        foreach (var template in PikoATrackLibrary.Templates)
        {
            TrackLibrary.Add(new TrackTemplateViewModel(template));
        }
    }
    #endregion

    #region Layout Data
    public ObservableCollection<TrackSegmentViewModel> Segments { get; } = [];
    public List<TrackConnection> Connections { get; } = [];

    [ObservableProperty]
    private TrackSegmentViewModel? selectedSegment;

    [ObservableProperty]
    private string layoutName = "Untitled Layout";

    [ObservableProperty]
    private string? layoutDescription;

    public bool HasSelectedSegment => SelectedSegment != null;
    public int SegmentCount => Segments.Count;
    public int AssignedFeedbackPointCount => Segments.Count(s => s.HasInPort);
    public string StatusText => $"{SegmentCount} segments, {AssignedFeedbackPointCount} with feedback points";

    partial void OnSelectedSegmentChanging(TrackSegmentViewModel? oldValue, TrackSegmentViewModel? newValue)
    {
        // Deselect old segment
        if (oldValue != null)
        {
            oldValue.IsSelected = false;
        }
        
        // Select new segment
        if (newValue != null)
        {
            newValue.IsSelected = true;
        }
    }

    partial void OnSelectedSegmentChanged(TrackSegmentViewModel? value)
    {
        _ = value;
        OnPropertyChanged(nameof(HasSelectedSegment));
        
        // Load InPort value from selected segment
        if (value?.AssignedInPort.HasValue == true)
        {
            InPortInput = value.AssignedInPort.Value;
        }
        else
        {
            InPortInput = double.NaN;
        }
    }
    #endregion

    #region Zoom & Pan

    /// <summary>
    /// Base scale factor calculated to fit the entire layout in view.
    /// When ZoomLevel = 100%, the layout fits perfectly in the canvas.
    /// </summary>
    [ObservableProperty]
    private double baseScale = 1.0;

    /// <summary>
    /// Zoom level in percent (5-400%). 100% = layout fits in view (baseScale).
    /// </summary>
    [ObservableProperty]
    private double zoomLevel = 100.0;

    /// <summary>
    /// Combined zoom factor for ScaleTransform = BaseScale * (ZoomLevel / 100).
    /// </summary>
    public double ZoomFactor => BaseScale * (ZoomLevel / 100.0);

    /// <summary>
    /// Display text for zoom level.
    /// </summary>
    public string ZoomLevelText => $"{ZoomLevel:F0}%";

    /// <summary>
    /// Pan offset X (canvas translation).
    /// </summary>
    [ObservableProperty]
    private double panOffsetX;

    /// <summary>
    /// Pan offset Y (canvas translation).
    /// </summary>
    [ObservableProperty]
    private double panOffsetY;

    partial void OnZoomLevelChanged(double value)
    {
        _ = value;
        OnPropertyChanged(nameof(ZoomFactor));
        OnPropertyChanged(nameof(ZoomLevelText));
    }

    partial void OnBaseScaleChanged(double value)
    {
        _ = value;
        OnPropertyChanged(nameof(ZoomFactor));
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(400, ZoomLevel + 5);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(5, ZoomLevel - 5);
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 100.0;
        PanOffsetX = 0;
        PanOffsetY = 0;
    }

    /// <summary>
    /// Pan the canvas by delta values.
    /// </summary>
    public void Pan(double deltaX, double deltaY)
    {
        PanOffsetX += deltaX;
        PanOffsetY += deltaY;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Add a new segment from the library.
    /// </summary>
    [RelayCommand]
    private void AddSegment(TrackTemplateViewModel? template)
    {
        if (template == null) return;

        var segment = new TrackSegment
        {
            ArticleCode = template.ArticleCode
        };

        var vm = new TrackSegmentViewModel(segment);
        Segments.Add(vm);
        SelectedSegment = vm;
        RenderLayout();

        Debug.WriteLine($"➕ Added segment: {template.ArticleCode}");
    }

    /// <summary>
    /// Delete the selected segment.
    /// </summary>
    [RelayCommand]
    private void DeleteSegment()
    {
        if (SelectedSegment == null) return;

        // Remove connections involving this segment
        Connections.RemoveAll(c =>
            c.Segment1Id == SelectedSegment.Id || c.Segment2Id == SelectedSegment.Id);

        Segments.Remove(SelectedSegment);
        SelectedSegment = null;
        RenderLayout();

        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// Connect two segments at their endpoints.
    /// </summary>
    [RelayCommand]
    private void ConnectSegments(string parameters)
    {
        // Format: "segment1Id,endpoint1,segment2Id,endpoint2"
        var parts = parameters.Split(',');
        if (parts.Length != 4) return;

        var connection = new TrackConnection
        {
            Segment1Id = parts[0],
            Segment1EndpointIndex = int.Parse(parts[1]),
            Segment2Id = parts[2],
            Segment2EndpointIndex = int.Parse(parts[3])
        };

        // Check for existing connection
        var exists = Connections.Any(c =>
            (c.Segment1Id == connection.Segment1Id && c.Segment2Id == connection.Segment2Id) ||
            (c.Segment1Id == connection.Segment2Id && c.Segment2Id == connection.Segment1Id));

        if (!exists)
        {
            Connections.Add(connection);
            RenderLayout();
        }
    }

    // Rotation commands removed - endpoints are imported from AnyRail

    /// <summary>
    /// Clear all segments.
    /// </summary>
    [RelayCommand]
    private void ClearLayout()
    {
        Segments.Clear();
        Connections.Clear();
        SelectedSegment = null;
        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// Disconnect the selected segment from all its connections.
    /// The segment becomes a free-floating piece that can be moved independently.
    /// </summary>
    [RelayCommand]
    private void DisconnectSegment()
    {
        if (SelectedSegment == null) return;

        var removedCount = Connections.RemoveAll(c =>
            c.Segment1Id == SelectedSegment.Id || c.Segment2Id == SelectedSegment.Id);

        if (removedCount > 0)
        {
            Debug.WriteLine($"🔗 Disconnected {SelectedSegment.ArticleCode}: removed {removedCount} connections");
            OnPropertyChanged(nameof(StatusText));
        }
    }

    /// <summary>
    /// Snap tolerance for endpoint matching (in layout units).
    /// </summary>
    private const double SnapTolerance = 50.0;  // Generous tolerance for user-friendly snapping

    /// <summary>
    /// Find nearby free endpoints that could snap to the given segment's endpoints.
    /// Returns the best snap candidate (closest match).
    /// </summary>
    public SnapCandidate? FindSnapCandidate(TrackSegmentViewModel draggedSegment)
    {
        if (draggedSegment.Model.Endpoints.Count == 0) return null;

        SnapCandidate? bestCandidate = null;
        double bestDistance = SnapTolerance;

        // Get all segments NOT in the current drag group
        var dragGroupIds = new HashSet<string>(FindConnectedGroup(draggedSegment.Id).Select(s => s.Id));

        foreach (var otherSegment in Segments)
        {
            // Skip segments in the drag group
            if (dragGroupIds.Contains(otherSegment.Id)) continue;

            // Check each endpoint of the dragged segment against each endpoint of other segment
            for (int dragEpIndex = 0; dragEpIndex < draggedSegment.Model.Endpoints.Count; dragEpIndex++)
            {
                var dragEp = draggedSegment.Model.Endpoints[dragEpIndex];

                for (int otherEpIndex = 0; otherEpIndex < otherSegment.Model.Endpoints.Count; otherEpIndex++)
                {
                    // Skip if this endpoint is already connected
                    if (IsEndpointConnected(otherSegment.Id, otherEpIndex)) continue;

                    var otherEp = otherSegment.Model.Endpoints[otherEpIndex];

                    // Calculate distance
                    var dx = dragEp.X - otherEp.X;
                    var dy = dragEp.Y - otherEp.Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCandidate = new SnapCandidate
                        {
                            TargetSegment = otherSegment,
                            TargetEndpointIndex = otherEpIndex,
                            DraggedEndpointIndex = dragEpIndex,
                            Distance = distance,
                            SnapX = otherEp.X,
                            SnapY = otherEp.Y
                        };
                    }
                }
            }
        }

        return bestCandidate;
    }

    /// <summary>
    /// Check if an endpoint is already connected.
    /// </summary>
    private bool IsEndpointConnected(string segmentId, int endpointIndex)
    {
        return Connections.Any(c =>
            (c.Segment1Id == segmentId && c.Segment1EndpointIndex == endpointIndex) ||
            (c.Segment2Id == segmentId && c.Segment2EndpointIndex == endpointIndex));
    }

    /// <summary>
    /// Create a connection when snapping occurs.
    /// </summary>
    public void SnapAndConnect(TrackSegmentViewModel draggedSegment, SnapCandidate candidate)
    {
        // Calculate offset to snap the dragged segment to the target
        var dragEp = draggedSegment.Model.Endpoints[candidate.DraggedEndpointIndex];
        var deltaX = candidate.SnapX - dragEp.X;
        var deltaY = candidate.SnapY - dragEp.Y;

        // Move all segments in the drag group
        var dragGroup = FindConnectedGroup(draggedSegment.Id);
        foreach (var segment in dragGroup)
        {
            segment.MoveBy(deltaX, deltaY);
        }

        // Create the connection
        var connection = new TrackConnection
        {
            Segment1Id = draggedSegment.Id,
            Segment1EndpointIndex = candidate.DraggedEndpointIndex,
            Segment2Id = candidate.TargetSegment.Id,
            Segment2EndpointIndex = candidate.TargetEndpointIndex
        };

        Connections.Add(connection);
        RefreshPathData();

        Debug.WriteLine($"🔗 Snapped {draggedSegment.ArticleCode} to {candidate.TargetSegment.ArticleCode}");
    }

    #endregion

    #region Rendering

        /// <summary>
        /// Render the layout by calculating positions from topology.
        /// </summary>
        private void RenderLayout()
        {
            // Build TrackLayout from current state
            var layout = new TrackLayout
            {
                Name = LayoutName,
                Description = LayoutDescription,
                Segments = Segments.Select(s => s.Model).ToList(),
                Connections = Connections.ToList()
            };

            Debug.WriteLine($"🎨 RenderLayout: {layout.Segments.Count} segments, {layout.Connections.Count} connections");

            // Calculate positions and path data from topology
            var rendered = _renderer.Render(layout);

            Debug.WriteLine($"✅ Rendered: {rendered.Count} segments");

            // Apply calculated positions, PathData, and rotation to ViewModels
            foreach (var rs in rendered)
            {
                var vm = Segments.FirstOrDefault(s => s.Id == rs.Id);
                if (vm != null)
                {
                    vm.X = rs.X;
                    vm.Y = rs.Y;
                    vm.PathData = rs.PathData;
                    vm.Rotation = rs.Rotation;  // Apply rotation for WinUI RenderTransform
                    var pathPreview = rs.PathData != null && rs.PathData.Length > 50 
                        ? rs.PathData.Substring(0, 50) + "..." 
                        : rs.PathData;
                    Debug.WriteLine($"  Segment {rs.Id} ({rs.ArticleCode}): X={rs.X:F1}, Y={rs.Y:F1}, PathData={pathPreview}");
                }
            }

            OnPropertyChanged(nameof(SegmentCount));
            OnPropertyChanged(nameof(StatusText));
        }

        /// <summary>
        /// Refresh PathData for all segments after drag/move operations.
        /// Uses current segment positions (Lines, Arcs) to regenerate SVG paths.
        /// </summary>
        public void RefreshPathData()
        {
            foreach (var vm in Segments)
            {
                vm.PathData = GeneratePathData(vm.Model);
            }
        }

        /// <summary>
        /// Generate SVG PathData from segment's Lines and Arcs.
        /// </summary>
        private static string GeneratePathData(TrackSegment segment)
        {
            var sb = new System.Text.StringBuilder();

            // Add lines
            foreach (var line in segment.Lines)
            {
                sb.Append($"M {line.X1:F1},{line.Y1:F1} L {line.X2:F1},{line.Y2:F1} ");
            }

            // Add arcs
            foreach (var arc in segment.Arcs)
            {
                // SVG Arc: M x1,y1 A rx,ry rotation large-arc-flag sweep-flag x2,y2
                var sweepFlag = arc.Sweep > 0 ? 1 : 0;
                sb.Append($"M {arc.X1:F1},{arc.Y1:F1} A {arc.Radius:F1},{arc.Radius:F1} 0 0 {sweepFlag} {arc.X2:F1},{arc.Y2:F1} ");
            }

            return sb.ToString().Trim();
        }

        #endregion

        #region Connection Graph

        /// <summary>
        /// Find all segments connected to the given segment (BFS traversal of connection graph).
        /// Returns a list including the start segment.
        /// </summary>
        public List<TrackSegmentViewModel> FindConnectedGroup(string startSegmentId)
        {
            var result = new List<TrackSegmentViewModel>();
            var visited = new HashSet<string>();
            var queue = new Queue<string>();

            queue.Enqueue(startSegmentId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (visited.Contains(currentId)) continue;
                visited.Add(currentId);

                var segment = Segments.FirstOrDefault(s => s.Id == currentId);
                if (segment != null)
                {
                    result.Add(segment);
                }

                // Find connected segments via Connections
                var connectedIds = Connections
                    .Where(c => c.Segment1Id == currentId || c.Segment2Id == currentId)
                    .Select(c => c.Segment1Id == currentId ? c.Segment2Id : c.Segment1Id)
                    .Where(id => !visited.Contains(id));

                foreach (var id in connectedIds)
                {
                    queue.Enqueue(id);
                }
            }

            return result;
        }



    #endregion

    #region InPort Assignment

    [ObservableProperty]
    private double inPortInput = double.NaN;

    [RelayCommand]
    private void AssignInPort()
    {
        if (SelectedSegment == null || double.IsNaN(InPortInput) || InPortInput < 1)
            return;

        SelectedSegment.AssignedInPort = (uint)InPortInput;
        OnPropertyChanged(nameof(AssignedFeedbackPointCount));
        OnPropertyChanged(nameof(StatusText));
    }

    [RelayCommand]
    private void ClearInPort()
    {
        if (SelectedSegment == null) return;
        SelectedSegment.AssignedInPort = null;
        InPortInput = double.NaN;
        OnPropertyChanged(nameof(AssignedFeedbackPointCount));
        OnPropertyChanged(nameof(StatusText));
    }

    #endregion

    #region Persistence

    private void SyncToProject()
    {
        var project = _mainViewModel.SelectedProject?.Model;
        if (project == null) return;




        project.TrackLayout ??= new TrackLayout();
        project.TrackLayout.Name = LayoutName;
        project.TrackLayout.Description = LayoutDescription;
        project.TrackLayout.Segments = Segments.Select(s => s.Model).ToList();
        project.TrackLayout.Connections = Connections.ToList();

        Debug.WriteLine($"💾 Saved: {Segments.Count} segments, {Connections.Count} connections");
    }

    private void LoadFromProject()
    {
        var layout = _mainViewModel.SelectedProject?.Model.TrackLayout;
        if (layout == null)
        {
            Debug.WriteLine("📂 No track layout in project");
            return;
        }

        Segments.Clear();
        Connections.Clear();

        LayoutName = layout.Name;
        LayoutDescription = layout.Description;

        foreach (var segment in layout.Segments)
        {
            Segments.Add(new TrackSegmentViewModel(segment));
        }

        Connections.AddRange(layout.Connections);
        
            // Always render to calculate PathData from topology
            RenderLayout();

            Debug.WriteLine($"📂 Loaded: {Segments.Count} segments, {Connections.Count} connections");
        }


        #endregion

        #region Import/Export

        /// <summary>
        /// Import AnyRail layout as PURE TOPOLOGY.
        /// Only ArticleCode + Connections are extracted - no coordinates.
        /// TopologyRenderer calculates all positions from track geometry library.
        /// </summary>
        [RelayCommand]
        private async Task ImportAnyRailAsync()
        {
            var file = await _ioService.BrowseForXmlFileAsync();
            if (file == null) return;

            var anyRailLayout = await AnyRailLayout.ParseAsync(file);

            Segments.Clear();
            Connections.Clear();

            // Build endpoint lookup for connection detection
            var endpointLookup = anyRailLayout.Endpoints.ToDictionary(e => e.Nr);

            // Convert AnyRail parts to segments (ArticleCode ONLY - no coordinates!)
            foreach (var part in anyRailLayout.Parts)
            {
                var articleCode = part.GetArticleCode();

                var segment = new TrackSegment
                {
                    Id = part.Id,
                    ArticleCode = articleCode
                    // NO Endpoints, Lines, Arcs - TopologyRenderer calculates from ArticleCode!
                };

                Segments.Add(new TrackSegmentViewModel(segment));
            }

            // Get connections directly from XML (explicit endpoint-to-endpoint mapping)
            var converted = anyRailLayout.ToTrackConnections();
            if (converted.Count == 0)
            {
                // Fallback: detect connections by grouping endpoints on parts
                Debug.WriteLine("⚠️ AnyRail ToTrackConnections returned 0 connections - falling back to DetectConnections()");
                DetectConnections(anyRailLayout, endpointLookup);
            }
            else
            {
                Connections.AddRange(converted);
            }

            // Log distinct article codes and missing geometries in library
            try
            {
                var codes = Segments.Select(s => s.ArticleCode).Distinct().OrderBy(c => c).ToList();
                var lib = new TrackGeometryLibrary();
                var missing = codes.Where(c => lib.GetGeometry(c) == null).ToList();
                Debug.WriteLine($"📦 Imported article codes: {string.Join(',', codes)}");
                if (missing.Count > 0)
                {
                    Debug.WriteLine($"⚠️ Missing geometries for article codes: {string.Join(',', missing)}");
                }
            }
            catch
            {
                // best-effort logging; don't crash import
            }

            // Set layout metadata
            LayoutName = "AnyRail Import";
            LayoutDescription = $"Imported {Segments.Count} segments, {Connections.Count} connections";

            // Reset view
            ZoomLevel = 100.0;
            PanOffsetX = 0;
            PanOffsetY = 0;

            // Render from topology (TopologyRenderer calculates all positions from geometry library)
            RenderLayout();

            Debug.WriteLine($"📂 Imported AnyRail: {Segments.Count} segments, {Connections.Count} connections");
        }

    /// <summary>
    /// Detect connections between segments based on shared AnyRail endpoints.
    /// </summary>
    private void DetectConnections(AnyRailLayout anyRailLayout, Dictionary<int, AnyRailEndpoint> endpointLookup)
    {
        // Group parts by endpoint numbers
        var endpointToParts = new Dictionary<int, List<(AnyRailPart Part, int EndpointIndex)>>();

        foreach (var part in anyRailLayout.Parts)
        {
            for (int i = 0; i < part.EndpointNrs.Count; i++)
            {
                var epNr = part.EndpointNrs[i];
                if (!endpointToParts.ContainsKey(epNr))
                {
                    endpointToParts[epNr] = [];
                }
                endpointToParts[epNr].Add((part, i));
            }
        }

        // Create connections where endpoints are shared
        foreach (var pair in endpointToParts.Values.Where(list => list.Count == 2))
        {
            var (part1, ep1) = pair[0];
            var (part2, ep2) = pair[1];

            Connections.Add(new TrackConnection
            {
                Segment1Id = part1.Id,
                Segment1EndpointIndex = ep1,
                Segment2Id = part2.Id,
                Segment2EndpointIndex = ep2
            });
        }
    }

    #endregion
}
