// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using Moba.TrackPlan.Domain;
using Moba.TrackPlan.Import.AnyRail;
using Moba.TrackPlan.Renderer;
using Moba.TrackPlan.Service;

using System.Collections.ObjectModel;

using TrackConnectionModel = Moba.TrackPlan.Domain.TrackConnection;
using TrackLayoutModel = Moba.TrackPlan.Domain.TrackLayout;
using TrackSegmentModel = Moba.TrackPlan.Domain.TrackSegment;

/// <summary>
        /// ViewModel for TrackPlanEditorPage - topology-based track plan editor.
        /// Positions and paths are calculated by TopologyRenderer from the connection graph.
        /// Feedback states managed by FeedbackStateManager (InPort → TrackSegment occupation).
        /// </summary>
public partial class TrackPlanEditorViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IIoService _ioService;
    private readonly TopologyRenderer _renderer;
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly FeedbackStateManager _feedbackStateManager;
    private readonly ILogger<TrackPlanEditorViewModel> _logger;

    /// <summary>
    /// Public accessor for FeedbackStateManager (used by JourneyManager for route monitoring).
    /// </summary>
    public FeedbackStateManager FeedbackStateManager => _feedbackStateManager;

    public TrackPlanEditorViewModel(MainWindowViewModel mainViewModel, IIoService ioService, ILogger<TrackPlanEditorViewModel> logger)
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;
        _logger = logger;
        _geometryLibrary = new TrackGeometryLibrary();
        _renderer = new TopologyRenderer(_geometryLibrary);
        _feedbackStateManager = new FeedbackStateManager();

        // Subscribe to feedback state changes
        _feedbackStateManager.FeedbackStateChanged += OnFeedbackStateChanged;

        _mainViewModel.SolutionSaving += (_, _) => SyncToProject();
        _mainViewModel.SolutionLoaded += (_, _) => LoadFromProject();
        _mainViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
                LoadFromProject();
        };

        LoadTrackLibrary();
    }

    /// <summary>
    /// Handle feedback state changes from Z21 InPort events.
    /// Updates UI to reflect occupied segments.
    /// </summary>
    private void OnFeedbackStateChanged(object? sender, FeedbackStateChangedEventArgs e)
    {
        var segment = Segments.FirstOrDefault(s => s.Id == e.SegmentId);
        if (segment != null)
        {
            // Trigger UI update by re-assigning IsOccupied (readonly property updates automatically)
            // No need to call OnPropertyChanged - IsOccupied is a computed property
            
            var stateText = e.IsOccupied ? "OCCUPIED" : "FREE";
            _logger.LogInformation("🚦 Segment {SegmentId} ({ArticleCode}) InPort {InPort} is now {State}",
                e.SegmentId, e.ArticleCode, e.InPort, stateText);
        }
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
    public List<TrackConnectionModel> Connections { get; } = [];

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

        var connection = new TrackConnectionModel
        {
            Segment1Id = parts[0],
            Segment1ConnectorIndex = int.Parse(parts[1]),
            Segment2Id = parts[2],
            Segment2ConnectorIndex = int.Parse(parts[3])
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

    /// <summary>
    /// Toggle switch state for parametric switch control.
    /// Changes active constraint without moving the switch or recalculating coordinates.
    /// Only affects which connection is active in the topology graph.
    /// </summary>
    [RelayCommand]
    private void ToggleSwitch()
    {
        if (SelectedSegment?.Model.SwitchState == null)
        {
            _logger.LogWarning("Selected segment is not a switch or has no SwitchState");
            return;
        }

        // Toggle switch state
        SelectedSegment.Model.SwitchState = SelectedSegment.Model.SwitchState switch
        {
            SwitchState.Straight => SwitchState.BranchLeft,
            SwitchState.BranchLeft => SwitchState.BranchRight,
            SwitchState.BranchRight => SwitchState.Straight,
            _ => SwitchState.Straight
        };

        _logger.LogInformation("Toggled switch {SegmentId} ({ArticleCode}) to {SwitchState}",
            SelectedSegment.Id, SelectedSegment.ArticleCode, SelectedSegment.Model.SwitchState);

        // Re-render layout with new active constraints
        // This only changes which connections are active, NOT the switch's WorldTransform
        RenderLayout();

        OnPropertyChanged(nameof(StatusText));
    }

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

    #endregion

    #region Rendering


    /// <summary>
    /// Render the layout by calculating WorldTransform matrices from topology.
    /// Pure topology-first: Renderer modifies Domain objects (TrackSegment.WorldTransform).
    /// </summary>
    private void RenderLayout()
    {
        // Build TrackLayout from current state
        var layout = new TrackLayoutModel
        {
            Name = LayoutName,
            Description = LayoutDescription,
            Segments = Segments.Select(s => s.Model).ToList(),
            Connections = Connections.ToList()
        };

        _logger.LogInformation("RenderLayout: {SegmentCount} segments, {ConnectionCount} connections", layout.Segments.Count, layout.Connections.Count);

        // Calculate WorldTransform for all segments (pure topology)
        _renderer.Render(layout);

        // Validate rendering results (debug builds only)
        #if DEBUG
        var validator = new GeometryValidator(_geometryLibrary);
        var validationErrors = new List<string>();
        validationErrors.AddRange(validator.ValidateLibrary());
        validationErrors.AddRange(validator.ValidateConnections(layout));
        foreach (var error in validationErrors)
        {
            _logger.LogWarning(error);
        }
        #endif

        // Update PathData for each ViewModel
        foreach (var vm in Segments)
        {
            var geometry = _geometryLibrary.GetGeometry(vm.ArticleCode);
            vm.PathData = geometry?.PathData ?? "M 0,0 L 30,0";
            
            // Trigger WorldTransform property changed (setter notifies UI)
            var currentTransform = vm.WorldTransform;
            vm.WorldTransform = currentTransform;
        }

        _logger.LogInformation("Rendered: {SegmentCount} segments with WorldTransform matrices", layout.Segments.Count);

        // Register segments with FeedbackStateManager for InPort monitoring
        _feedbackStateManager.RegisterSegments(layout.Segments);

        // TODO: Calculate canvas size from bounding box of transformed segments
        // For now: Use default size
        CanvasWidth = 1200;
        CanvasHeight = 800;

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
        private string GeneratePathData(TrackSegmentModel segment)
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

        var layout = project.TrackLayout ??= new TrackLayoutModel();
        layout.Name = LayoutName;
        layout.Description = LayoutDescription;
        layout.Segments = Segments.Select(s => s.Model).ToList();
        layout.Connections = Connections.ToList();

        _logger.LogInformation("Saved track layout: {SegmentCount} segments, {ConnectionCount} connections", Segments.Count, Connections.Count);
    }

    private void LoadFromProject()
    {
        var layout = _mainViewModel.SelectedProject?.Model.TrackLayout;
        if (layout is not TrackLayoutModel trackLayout)
        {
            _logger.LogInformation("No track layout in project");
            return;
        }

        Segments.Clear();
        Connections.Clear();

        LayoutName = trackLayout.Name;
        LayoutDescription = trackLayout.Description;

        foreach (var segment in trackLayout.Segments)
        {
            Segments.Add(new TrackSegmentViewModel(segment));
        }

        Connections.AddRange(trackLayout.Connections);
        
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

        _logger.LogInformation("📦 Importing AnyRail layout: {PartCount} parts, {EndpointCount} endpoints, {ConnectionCount} connections",
            anyRailLayout.Parts.Count, anyRailLayout.Endpoints.Count, anyRailLayout.Connections.Count);

        Segments.Clear();
        Connections.Clear();

        var geometryLibrary = new TrackGeometryLibrary();
        var missingGeometries = new HashSet<string>();

        // Step 1: Create segments (pure topology - no coordinates)
        int skippedCount = 0;
        foreach (var part in anyRailLayout.Parts)
        {
            var articleCode = part.GetArticleCode();
            
            // Verify geometry exists
            var geometry = geometryLibrary.GetGeometry(articleCode);
            if (geometry == null)
            {
                missingGeometries.Add(articleCode);
                _logger.LogWarning("❌ Missing geometry for {ArticleCode} (Part {PartId}) - SKIPPING", 
                    articleCode, part.Id);
                skippedCount++;
                continue;
            }
            
            _logger.LogDebug("✅ Part {PartId} ({ArticleCode}): {ConnectorCount} connectors, EndpointNrs: [{EndpointNrs}]", 
                part.Id, articleCode, geometry.Endpoints.Count, string.Join(",", part.EndpointNrs));
            
            // Create TrackSegment (pure topology)
            var segment = new TrackSegmentModel
            {
                Id = part.Id,
                ArticleCode = articleCode
            };
            
            Segments.Add(new TrackSegmentViewModel(segment));
        }

        if (skippedCount > 0)
        {
            _logger.LogWarning("⚠️ Skipped {SkippedCount}/{TotalCount} parts due to missing geometries", 
                skippedCount, anyRailLayout.Parts.Count);
        }

        // Step 2: Create connections using ToTrackConnections (direct EndpointNrs index mapping)
        var connections = anyRailLayout.ToTrackConnections();
        
        // Validate connector indices before adding
        int validConnections = 0;
        foreach (var conn in connections)
        {
            var seg1 = Segments.FirstOrDefault(s => s.Id == conn.Segment1Id);
            var seg2 = Segments.FirstOrDefault(s => s.Id == conn.Segment2Id);
            
            if (seg1 == null || seg2 == null)
                continue; // Segment was skipped due to missing geometry
            
            var geom1 = geometryLibrary.GetGeometry(seg1.ArticleCode);
            var geom2 = geometryLibrary.GetGeometry(seg2.ArticleCode);
            
            // Hard validation: Connector index must be within bounds
            if (geom1 != null && conn.Segment1ConnectorIndex >= geom1.Endpoints.Count)
            {
                _logger.LogError("❌ Invalid ConnectorIndex {ConnIdx} for {SegmentId} ({ArticleCode}) - geometry only has {Count} connectors",
                    conn.Segment1ConnectorIndex, seg1.Id, seg1.ArticleCode, geom1.Endpoints.Count);
                continue;
            }
            
            if (geom2 != null && conn.Segment2ConnectorIndex >= geom2.Endpoints.Count)
            {
                _logger.LogError("❌ Invalid ConnectorIndex {ConnIdx} for {SegmentId} ({ArticleCode}) - geometry only has {Count} connectors",
                    conn.Segment2ConnectorIndex, seg2.Id, seg2.ArticleCode, geom2.Endpoints.Count);
                continue;
            }
            
            Connections.Add(conn);
            validConnections++;
            
            if (validConnections <= 10)
            {
                _logger.LogInformation("🔗 Connection #{Index}: {Seg1} ({Art1})[{Conn1}] ↔ {Seg2} ({Art2})[{Conn2}]",
                    validConnections, seg1.Id, seg1.ArticleCode, conn.Segment1ConnectorIndex,
                    seg2.Id, seg2.ArticleCode, conn.Segment2ConnectorIndex);
            }
        }
        
        _logger.LogInformation("🔗 Created {ValidCount}/{TotalCount} valid connections",
            validConnections, connections.Count);

        // Report missing geometries
        if (missingGeometries.Count > 0)
        {
            _logger.LogWarning("❌ Missing geometries: {MissingGeometries}", 
                string.Join(", ", missingGeometries));
        }

        // Step 3: Render layout (TopologyRenderer computes WorldTransform from topology)
        RenderLayout();

        _logger.LogInformation("✅ Imported AnyRail layout: {SegmentCount} segments, {ConnectionCount} connections", 
             Segments.Count, Connections.Count);
    }

    #endregion
}
