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
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for TrackPlanEditorPage - topology-based track plan editor.
/// Positions and paths are calculated by TopologyRenderer from the connection graph.
/// </summary>
public partial class TrackPlanEditorViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IIoService _ioService;
    private readonly TrackLayoutRenderer _renderer;
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly ILogger<TrackPlanEditorViewModel> _logger;

    public TrackPlanEditorViewModel(MainWindowViewModel mainViewModel, IIoService ioService, ILogger<TrackPlanEditorViewModel> logger)
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;
        _logger = logger;
        _geometryLibrary = new TrackGeometryLibrary();
        _renderer = new TrackLayoutRenderer(_geometryLibrary);

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

    [ObservableProperty]
    private double canvasWidth = 2000;

    [ObservableProperty]
    private double canvasHeight = 1500;

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

        _logger.LogInformation("Added segment: {ArticleCode}", template.ArticleCode);
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
             _logger.LogInformation("Disconnected {ArticleCode}: removed {RemovedConnectionCount} connections", SelectedSegment.ArticleCode, removedCount);
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
    /// TODO: Reimplement with topology-first architecture using TrackGeometryLibrary
    /// </summary>
    public SnapCandidate? FindSnapCandidate(TrackSegmentViewModel draggedSegment)
    {
        // Temporarily disabled - needs reimplementation with TrackGeometryLibrary
        // Old implementation used Endpoints[] which no longer exists in Domain
        return null;
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
    /// TODO: Reimplement with topology-first architecture
    /// </summary>
    public void SnapAndConnect(TrackSegmentViewModel draggedSegment, SnapCandidate candidate)
    {
        // Temporarily disabled - needs reimplementation with TrackGeometryLibrary
        _logger.LogWarning("SnapAndConnect not yet implemented in topology-first architecture");
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

            _logger.LogInformation("RenderLayout: {SegmentCount} segments, {ConnectionCount} connections", layout.Segments.Count, layout.Connections.Count);

            // Calculate positions and path data from topology
            var result = _renderer.Render(layout);

            _logger.LogInformation("Rendered: {SegmentCount} segments", result.Segments.Count);

            // Apply calculated positions, PathData, and rotation to ViewModels
            int successCount = 0;
            int errorCount = 0;
            
            foreach (var rs in result.Segments)
            {
                var vm = Segments.FirstOrDefault(s => s.Id == rs.Id);
                if (vm != null)
                {
                    try
                    {
                        vm.X = rs.X;
                        vm.Y = rs.Y;
                        vm.PathData = rs.PathData;
                        vm.Rotation = rs.Rotation;
                        successCount++;
                        
                        var pathPreview = rs.PathData != null && rs.PathData.Length > 50 
                            ? rs.PathData.Substring(0, 50) + "..." 
                            : rs.PathData;
                        _logger.LogDebug("Applied render result: Segment {SegmentId} ({ArticleCode}): X={X:F1}, Y={Y:F1}, Rotation={Rotation:F1}°, PathData={PathDataLength} chars", 
                            rs.Id, rs.ArticleCode, rs.X, rs.Y, rs.Rotation, rs.PathData?.Length ?? 0);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        var pathPreview = rs.PathData != null && rs.PathData.Length > 100
                            ? rs.PathData.Substring(0, 100) + "..."
                            : rs.PathData;
                        _logger.LogError(ex, "Failed to apply render result for segment {SegmentId} ({ArticleCode}): X={X}, Y={Y}, Rotation={Rotation}, PathDataPreview={PathDataPreview}",
                            rs.Id, rs.ArticleCode, rs.X, rs.Y, rs.Rotation, pathPreview ?? "null");
                    }
                }
            }

            _logger.LogInformation("Segment render results applied: {SuccessCount} successful, {ErrorCount} errors", successCount, errorCount);

            if (result.Segments.Count > 0)
            {
                var bb = result.BoundingBox;
                
                // ALWAYS normalize coordinates: shift all segments so minX=0, minY=0
                const double padding = 50.0;
                var offsetX = -bb.MinX + padding;
                var offsetY = -bb.MinY + padding;
                
                _logger.LogInformation("🔧 Normalizing coordinates: offset=({OffsetX:F1}, {OffsetY:F1}), BoundingBox=({MinX:F1}, {MinY:F1}) to ({MaxX:F1}, {MaxY:F1})", 
                    offsetX, offsetY, bb.MinX, bb.MinY, bb.MaxX, bb.MaxY);
                
                // Apply offset to all segments
                foreach (var vm in Segments)
                {
                    vm.X += offsetX;
                    vm.Y += offsetY;
                }
                
                // Update canvas size (width/height + 2*padding for margins)
                CanvasWidth = Math.Max(bb.Width + 2 * padding, 800);
                CanvasHeight = Math.Max(bb.Height + 2 * padding, 600);
                
                _logger.LogInformation("🔧 Canvas size set to {CanvasWidth:F1} x {CanvasHeight:F1} (bounding {BoundingBoxWidth:F1} x {BoundingBoxHeight:F1})", 
                    CanvasWidth, CanvasHeight, bb.Width, bb.Height);
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
        /// Generate SVG PathData from segment's ArticleCode using TrackGeometryLibrary.
        /// Topology-first: No coordinates in Domain, use geometry library.
        /// </summary>
        private string GeneratePathData(TrackSegment segment)
        {
            // Generate from TrackGeometryLibrary (topology-first approach)
            var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
            return geometry?.PathData ?? "M 0,0";  // Empty path if geometry missing
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

        _logger.LogInformation("Saved track layout: {SegmentCount} segments, {ConnectionCount} connections", Segments.Count, Connections.Count);
    }

    private void LoadFromProject()
    {
        var layout = _mainViewModel.SelectedProject?.Model.TrackLayout;
        if (layout == null)
        {
            _logger.LogInformation("No track layout in project");
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
        
        // All layouts now use topology-based rendering (no geometry stored in Domain)
        _logger.LogInformation("Loading track layout with topology-based rendering");
        RenderLayout();

        _logger.LogInformation("Loaded track layout: {SegmentCount} segments, {ConnectionCount} connections", Segments.Count, Connections.Count);
    }


        #endregion

        #region Import/Export

    [RelayCommand]
    private async Task ImportFromAnyRailXmlAsync()
    {
        var file = await _ioService.BrowseForXmlFileAsync();
        if (file == null) return;

        var anyRailLayout = await AnyRailLayout.ParseAsync(file);

        Segments.Clear();
        Connections.Clear();

        // Get TrackGeometryLibrary for ArticleCode validation
        var geometryLibrary = new TrackGeometryLibrary();
        var missingGeometries = new HashSet<string>();

        // Step 1: Convert AnyRail parts to segments WITH endpoint world coordinates from AnyRail XML
        var segmentData = new List<(string SegmentId, string ArticleCode, List<(double X, double Y, double Heading)> EndpointCoords)>();
        
        foreach (var part in anyRailLayout.Parts)
        {
            var articleCode = part.GetArticleCode();
            
            // Verify geometry exists in library
            var geometry = geometryLibrary.GetGeometry(articleCode);
            if (geometry == null)
            {
                missingGeometries.Add(articleCode);
                _logger.LogWarning("Missing geometry for article code: {ArticleCode} (Part {PartId}) - SKIPPING", articleCode, part.Id);
                continue;
            }
            
            var segment = new TrackSegment
            {
                Id = part.Id,
                ArticleCode = articleCode
                // Pure topology - no coordinates stored
            };
            
            // ViewModel will be populated by RenderLayout() after all segments are imported
            var vm = new TrackSegmentViewModel(segment);
            Segments.Add(vm);
            
            // Get endpoint world coordinates from AnyRail XML (NOT calculated from Part position!)
            var endpointCoords = part.GetEndpointWorldCoordinates(anyRailLayout);
            segmentData.Add((part.Id, articleCode, endpointCoords));
            
            _logger.LogInformation("Part {PartId} ({ArticleCode}): {EndpointCount} endpoints from AnyRail XML", 
                part.Id, articleCode, endpointCoords.Count);
        }

        // Step 2: Use ConnectorMatcher to find connections based on AnyRail endpoint coordinates
        var connectorMatcher = new ConnectorMatcher();
        var connections = connectorMatcher.MatchConnectorsFromEndpoints(segmentData, geometryLibrary);
        Connections.AddRange(connections);
        
        _logger.LogInformation("ConnectorMatcher found {ConnectionCount} connections from {SegmentCount} segments", 
            connections.Count, segmentData.Count);

        // Report missing geometries
        if (missingGeometries.Count > 0)
        {
            _logger.LogWarning("Missing geometries in library: {MissingGeometries}. Add these to TrackGeometryLibrary for correct rendering", 
                string.Join(", ", missingGeometries));
        }

        // Step 3: Render layout to calculate world positions from topology
        // (Discard temporary coordinates - renderer calculates fresh from graph)
        RenderLayout();

        _logger.LogInformation("Imported AnyRail layout using Track-Graph Architecture: {ImportedSegmentCount}/{TotalSegmentCount} segments, {ConnectionCount} connections", 
             Segments.Count, anyRailLayout.Parts.Count, Connections.Count);
    }

    /// <summary>
    /// Find the AnyRail part that owns a specific endpoint number.
    /// </summary>
    private AnyRailPart? FindSegmentByEndpointNr(List<AnyRailPart> parts, int endpointNr)
    {
        return parts.FirstOrDefault(p => p.EndpointNrs.Contains(endpointNr));
    }

    /// <summary>
    /// Import AnyRail layout and convert to PURE TOPOLOGY (no coordinates).
    /// Renderer calculates all positions from ArticleCode + Connections.
    /// </summary>
    [RelayCommand]
    private async Task ImportAnyRailPureTopologyAsync()
    {
        var file = await _ioService.BrowseForXmlFileAsync();
        if (file == null) return;

        var anyRailLayout = await AnyRailLayout.ParseAsync(file);

        Segments.Clear();
        Connections.Clear();

        // Convert AnyRail parts to segments (ArticleCode ONLY)
        foreach (var part in anyRailLayout.Parts)
        {
             var articleCode = part.GetArticleCode();
             
             // Register dynamic geometry if not already in library (turnouts, DKW, W3)
             if (_geometryLibrary.GetGeometry(articleCode) == null)
             {
                 var endpoints = part.GetEndpoints().Select(e => new TrackPoint(e.X, e.Y)).ToList();
                 var headings = part.CalculateEndpointHeadings(anyRailLayout); // Pass parent layout for endpoint lookup
                 var pathData = part.ToPathData();

                 _geometryLibrary.RegisterDynamicGeometry(articleCode, endpoints, headings, pathData);
                 _logger.LogDebug("Registered dynamic geometry: {ArticleCode} with {EndpointCount} endpoints", articleCode, endpoints.Count);
             }
             
             var segment = new TrackSegment
             {
                 Id = part.Id,
                 ArticleCode = articleCode
                 // Pure topology: no coordinates
             };

             Segments.Add(new TrackSegmentViewModel(segment));
             _logger.LogDebug("Imported segment: {PartId} -> {ArticleCode}", part.Id, articleCode);
         }

        // Detect and create connections based on shared AnyRail endpoints
        var connections = anyRailLayout.ToTrackConnections();
        Connections.AddRange(connections);
        _logger.LogDebug("Converted {ConnectionCount} connections from AnyRail XML", connections.Count);

        // Set layout metadata
        LayoutName = "AnyRail Import";
        LayoutDescription = $"Imported {Segments.Count} segments, {Connections.Count} connections";

        // Reset view
        ZoomLevel = 100.0;
        PanOffsetX = 0;
        PanOffsetY = 0;

        // Render from topology (TopologyRenderer calculates all positions from geometry library)
        RenderLayout();

        _logger.LogInformation("Imported AnyRail (pure topology): {SegmentCount} segments, {ConnectionCount} connections", Segments.Count, Connections.Count);
    }

    #endregion
}
