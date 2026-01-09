// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using Moba.TrackPlan.Service;

using Service;

using System.Collections.ObjectModel;

using TrackPlan.Geometry;
using TrackPlan.Import;
using TrackPlan.Renderer;

using TrackConnectionModel = Moba.TrackPlan.Domain.TrackConnection;
using TrackLayoutModel = Moba.TrackPlan.Domain.TrackLayout;
using TrackSegmentModel = Moba.TrackPlan.Domain.TrackSegment;

public partial class TrackPlanEditorViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IIoService _ioService;
    private readonly TopologyRenderer _renderer;
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly FeedbackStateManager _feedbackStateManager;
    private readonly ILogger<TrackPlanEditorViewModel> _logger;

    private readonly TopologySolver _topologySolver;

    public TrackPlanEditorViewModel(
        MainWindowViewModel mainViewModel,
        IIoService ioService,
        TopologyRenderer renderer,
        TrackGeometryLibrary geometryLibrary,
        FeedbackStateManager feedbackStateManager,
        TopologySolver topologySolver,
        ILogger<TrackPlanEditorViewModel> logger)
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;
        _renderer = renderer;
        _geometryLibrary = geometryLibrary;
        _feedbackStateManager = feedbackStateManager;
        _topologySolver = topologySolver;
        _logger = logger;

        // Subscribe to Solution lifecycle events for persistence
        _mainViewModel.SolutionSaving += OnSolutionSaving;
        _mainViewModel.SolutionLoaded += OnSolutionLoaded;

        // Load Piko A-Gleis track library
        foreach (var template in Moba.TrackPlan.Domain.PikoATrackLibrary.Templates)
        {
            TrackLibrary.Add(new TrackTemplateViewModel(template));
        }

        _logger.LogInformation("Loaded {Count} Piko A-Gleis templates", TrackLibrary.Count);

        // Load existing data if Solution was already loaded before this ViewModel was created
        LoadFromProject();
    }

    private void OnSolutionSaving(object? sender, EventArgs e)
    {
        SaveToProject();
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
        LoadFromProject();
    }

    // --------------------------------------------------------------------
    // Track Library
    // --------------------------------------------------------------------

    public ObservableCollection<TrackTemplateViewModel> TrackLibrary { get; } = [];

    [ObservableProperty]
    private TrackTemplateViewModel? selectedTemplate;

    // --------------------------------------------------------------------
    // Layout Data
    // --------------------------------------------------------------------

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
        if (oldValue != null)
            oldValue.IsSelected = false;

        if (newValue != null)
            newValue.IsSelected = true;
    }

    partial void OnSelectedSegmentChanged(TrackSegmentViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedSegment));

        if (value?.AssignedInPort.HasValue == true)
            InPortInput = value.AssignedInPort.Value;
        else
            InPortInput = double.NaN;
    }

    // --------------------------------------------------------------------
    // Zoom & Pan
    // --------------------------------------------------------------------

    [ObservableProperty]
    private double baseScale = 1.0;

    [ObservableProperty]
    private double zoomLevel = 100.0;

    public double ZoomFactor => BaseScale * (ZoomLevel / 100.0);
    public string ZoomLevelText => $"{ZoomLevel:F0}%";

    [ObservableProperty]
    private double panOffsetX;

    [ObservableProperty]
    private double panOffsetY;

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomFactor));
        OnPropertyChanged(nameof(ZoomLevelText));
    }

    partial void OnBaseScaleChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomFactor));
    }

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(400, ZoomLevel + 5);

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(5, ZoomLevel - 5);

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 100.0;
        PanOffsetX = 0;
        PanOffsetY = 0;
    }

    public void Pan(double dx, double dy)
    {
        PanOffsetX += dx;
        PanOffsetY += dy;
    }

    // --------------------------------------------------------------------
    // Commands
    // --------------------------------------------------------------------

    /// <summary>
    /// Counter for positioning new unconnected segments in a grid pattern.
    /// </summary>
    private int _newSegmentCounter;

    [RelayCommand]
    private void AddSegment(TrackTemplateViewModel? template)
    {
        if (template == null) return;

        // Calculate initial position for new segment
        // Place in a grid pattern starting from center of canvas
        const double startX = 300.0;
        const double startY = 200.0;
        const double spacingX = 250.0;
        const double spacingY = 150.0;
        const int columnsPerRow = 5;

        var col = _newSegmentCounter % columnsPerRow;
        var row = _newSegmentCounter / columnsPerRow;
        var initialX = startX + col * spacingX;
        var initialY = startY + row * spacingY;

        _newSegmentCounter++;

        var segment = new TrackSegmentModel
        {
            Id = Guid.NewGuid().ToString(),
            ArticleCode = template.ArticleCode,
            WorldTransform = new Transform2D
            {
                TranslateX = initialX,
                TranslateY = initialY,
                RotationDegrees = 0
            }
        };

        var vm = new TrackSegmentViewModel(segment);
        Segments.Add(vm);
        SelectedSegment = vm;

        // Only update PathData for the new segment, don't re-solve topology
        vm.PathData = GeneratePathData(segment);

        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));
    }

    [RelayCommand]
    private void DeleteSegment()
    {
        if (SelectedSegment == null) return;

        Connections.RemoveAll(c =>
            c.Segment1Id == SelectedSegment.Id ||
            c.Segment2Id == SelectedSegment.Id);

        Segments.Remove(SelectedSegment);
        SelectedSegment = null;

        RenderLayout();
    }

    [RelayCommand]
    private void ClearLayout()
    {
        Segments.Clear();
        Connections.Clear();
        SelectedSegment = null;

        RenderLayout();
    }

    [RelayCommand]
    private void DisconnectSegment()
    {
        if (SelectedSegment == null) return;

        Connections.RemoveAll(c =>
            c.Segment1Id == SelectedSegment.Id ||
            c.Segment2Id == SelectedSegment.Id);

        RenderLayout();
    }

    // --------------------------------------------------------------------
    // Rendering
    // --------------------------------------------------------------------

    private void RenderLayout()
    {
        _topologySolver.Solve(Segments, Connections);

        // Domain-Layout erzeugen
        var layout = new TrackLayoutModel
        {
            Name = LayoutName,
            Description = LayoutDescription,
            Segments = Segments.Select(s => s.Model).ToList(),
            Connections = Connections.ToList()
        };

        // Renderer anwenden
        _renderer.Render(layout);

        // PathData aktualisieren
        foreach (var vm in Segments)
            vm.PathData = GeneratePathData(vm.Model);

        // Feedback aktualisieren
        _feedbackStateManager.RegisterSegments(layout.Segments);

        // Canvas aktualisieren
        CanvasWidth = 1200;
        CanvasHeight = 800;

        // UI aktualisieren
        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));
    }

    private void RenderLayout(string preferredRootId)
    {
        // Solve topology with specified root segment (keeps its position as anchor)
        _topologySolver.Solve(Segments, Connections, preferredRootId);

        // Domain-Layout erzeugen
        var layout = new TrackLayoutModel
        {
            Name = LayoutName,
            Description = LayoutDescription,
            Segments = Segments.Select(s => s.Model).ToList(),
            Connections = Connections.ToList()
        };

        // Renderer anwenden
        _renderer.Render(layout);

        // PathData aktualisieren
        foreach (var vm in Segments)
            vm.PathData = GeneratePathData(vm.Model);

        // Feedback aktualisieren
        _feedbackStateManager.RegisterSegments(layout.Segments);

        // Canvas aktualisieren
        CanvasWidth = 1200;
        CanvasHeight = 800;

        // UI aktualisieren
        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));
    }

    private string GeneratePathData(TrackSegmentModel segment)
    {
        var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
        if (geometry == null)
            return "M 0,0";

        // Apply WorldTransform to the path data
        var wt = segment.WorldTransform;
        return PathDataTransformer.Transform(
            geometry.PathData,
            wt.TranslateX,
            wt.TranslateY,
            wt.RotationDegrees);
    }

    // --------------------------------------------------------------------
    // Persistence: Save/Load to Project
    // --------------------------------------------------------------------

    private void SaveToProject()
    {
        var project = _mainViewModel.Solution?.Projects?.FirstOrDefault();
        if (project == null)
        {
            _logger.LogWarning("Cannot save TrackLayout: No project available");
            return;
        }

        var layout = new TrackLayoutModel
        {
            Name = LayoutName,
            Description = LayoutDescription,
            Segments = Segments.Select(s => s.Model).ToList(),
            Connections = Connections.ToList()
        };

        project.TrackLayout = layout;
        _logger.LogInformation("Saved TrackLayout with {SegmentCount} segments, {ConnectionCount} connections",
            layout.Segments.Count, layout.Connections.Count);
    }

    private void LoadFromProject()
    {
        var project = _mainViewModel.Solution?.Projects?.FirstOrDefault();
        if (project?.TrackLayout == null)
        {
            _logger.LogDebug("No TrackLayout in project to load");
            return;
        }

        var layout = project.TrackLayout;

        // Clear current state
        Segments.Clear();
        Connections.Clear();
        _newSegmentCounter = 0;

        // Load segments
        foreach (var segmentModel in layout.Segments)
        {
            var vm = new TrackSegmentViewModel(segmentModel);
            vm.PathData = GeneratePathData(segmentModel);
            Segments.Add(vm);
        }

        // Load connections
        Connections.AddRange(layout.Connections);

        // Update properties
        LayoutName = layout.Name;
        LayoutDescription = layout.Description;

        // Register feedback points
        _feedbackStateManager.RegisterSegments(layout.Segments);

        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));

        _logger.LogInformation("Loaded TrackLayout with {SegmentCount} segments, {ConnectionCount} connections",
            Segments.Count, Connections.Count);
    }

    public void RefreshPathData()
    {
        foreach (var vm in Segments)
            vm.PathData = GeneratePathData(vm.Model);
    }

    // --------------------------------------------------------------------
    // Connection Graph
    // --------------------------------------------------------------------

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
                result.Add(segment);

            var connectedIds = Connections
                .Where(c => c.Segment1Id == currentId || c.Segment2Id == currentId)
                .Select(c => c.Segment1Id == currentId ? c.Segment2Id : c.Segment1Id)
                .Where(id => !visited.Contains(id));

            foreach (var id in connectedIds)
                queue.Enqueue(id);
        }

        return result;
    }

    // --------------------------------------------------------------------
    // InPort Assignment
    // --------------------------------------------------------------------

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

    [RelayCommand]
    private async Task ImportFromAnyRailXml()
    {
        var file = await _ioService.BrowseForXmlFileAsync();
        if (file == null) return;

        var anyRailLayout = await AnyRail.ParseAsync(file);

        _logger.LogInformation(
            "Importing AnyRail layout: {PartCount} parts, {EndpointCount} endpoints, {ConnectionCount} connections",
            anyRailLayout.Parts.Count, anyRailLayout.Endpoints.Count, anyRailLayout.Connections.Count);

        // 1) Clear current layout
        Segments.Clear();
        Connections.Clear();

        // 2) Create segments
        foreach (var part in anyRailLayout.Parts)
        {
            var articleCode = part.GetArticleCode();
            var geometry = _geometryLibrary.GetGeometry(articleCode);
            if (geometry == null)
            {
                _logger.LogWarning("Missing geometry for {ArticleCode}", articleCode);
                continue;
            }

            var segment = new TrackSegmentModel
            {
                Id = part.Id,
                ArticleCode = articleCode,
                WorldTransform = Transform2D.Identity
            };

            Segments.Add(new TrackSegmentViewModel(segment));
        }

        // 3) Convert AnyRail endpoint-connections to TrackConnections
        var converted = anyRailLayout.ToTrackConnections();
        Connections.AddRange(converted);

        _logger.LogInformation("Converted {Count} AnyRail connections", converted.Count);

        // 4) Solve topology
        _topologySolver.Solve(Segments, Connections);

        // 5) Render layout
        var layout = new TrackLayoutModel
        {
            Name = LayoutName,
            Description = LayoutDescription,
            Segments = Segments.Select(s => s.Model).ToList(),
            Connections = Connections.ToList()
        };

        _renderer.Render(layout);

        // 6) Update PathData
        foreach (var vm in Segments)
            vm.PathData = GeneratePathData(vm.Model);

        _feedbackStateManager.RegisterSegments(layout.Segments);

        CanvasWidth = 1200;
        CanvasHeight = 800;

        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));

        _logger.LogInformation(
            "Imported AnyRail layout: {SegmentCount} segments, {ConnectionCount} connections",
            Segments.Count, Connections.Count);
    }

    // --------------------------------------------------------------------
    // Connector Docking (Snap-to-Connect)
    // --------------------------------------------------------------------

    private const double SnapThresholdMm = 20.0;

    /// <summary>
    /// Attempts to connect a segment to the nearest available connector.
    /// Returns true if a connection was made.
    /// </summary>
    public bool TrySnapToNearestConnector(TrackSegmentViewModel draggedSegment)
    {
        var draggedGeom = _geometryLibrary.GetGeometry(draggedSegment.ArticleCode);
        if (draggedGeom == null) return false;

        double bestDistance = double.MaxValue;
        TrackSegmentViewModel? bestTarget = null;
        int bestDraggedConnectorIdx = -1;
        int bestTargetConnectorIdx = -1;

        // Get world positions of dragged segment's connectors
        var draggedConnectors = GetWorldConnectorPositions(draggedSegment, draggedGeom);

        foreach (var targetSegment in Segments)
        {
            if (targetSegment.Id == draggedSegment.Id) continue;

            // Skip if already connected to this segment
            if (IsConnected(draggedSegment.Id, targetSegment.Id)) continue;

            var targetGeom = _geometryLibrary.GetGeometry(targetSegment.ArticleCode);
            if (targetGeom == null) continue;

            var targetConnectors = GetWorldConnectorPositions(targetSegment, targetGeom);

            // Find closest pair of connectors
            for (int di = 0; di < draggedConnectors.Count; di++)
            {
                // Skip if this dragged connector is already used
                if (IsConnectorUsed(draggedSegment.Id, di)) continue;

                for (int ti = 0; ti < targetConnectors.Count; ti++)
                {
                    // Skip if this target connector is already used
                    if (IsConnectorUsed(targetSegment.Id, ti)) continue;

                    var distance = Distance(draggedConnectors[di], targetConnectors[ti]);
                    if (distance < SnapThresholdMm && distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = targetSegment;
                        bestDraggedConnectorIdx = di;
                        bestTargetConnectorIdx = ti;
                    }
                }
            }
        }

        if (bestTarget != null && bestDraggedConnectorIdx >= 0 && bestTargetConnectorIdx >= 0)
        {
            // Create connection
            var connection = new TrackConnectionModel
            {
                Segment1Id = draggedSegment.Id,
                Segment1ConnectorIndex = bestDraggedConnectorIdx,
                Segment2Id = bestTarget.Id,
                Segment2ConnectorIndex = bestTargetConnectorIdx,
                ConstraintType = TrackPlan.Domain.ConstraintType.Rigid
            };

            Connections.Add(connection);
            _logger.LogInformation(
                "Connected {Seg1}[{Conn1}] to {Seg2}[{Conn2}] (distance: {Dist:F1}mm)",
                draggedSegment.ArticleCode, bestDraggedConnectorIdx,
                bestTarget.ArticleCode, bestTargetConnectorIdx,
                bestDistance);

            // Use the target segment as root (it has the established position)
            RenderLayout(bestTarget.Id);
            return true;
        }

        return false;
    }

    private List<(double X, double Y)> GetWorldConnectorPositions(
        TrackSegmentViewModel segment,
        TrackPlan.Domain.TrackGeometry geometry)
    {
        var result = new List<(double X, double Y)>();
        var wt = segment.WorldTransform;

        foreach (var endpoint in geometry.Endpoints)
        {
            // Transform local to world coordinates
            var radians = wt.RotationDegrees * Math.PI / 180.0;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);

            var worldX = wt.TranslateX + endpoint.X * cos - endpoint.Y * sin;
            var worldY = wt.TranslateY + endpoint.X * sin + endpoint.Y * cos;

            result.Add((worldX, worldY));
        }

        return result;
    }

    private bool IsConnected(string segmentId1, string segmentId2)
    {
        return Connections.Any(c =>
            (c.Segment1Id == segmentId1 && c.Segment2Id == segmentId2) ||
            (c.Segment1Id == segmentId2 && c.Segment2Id == segmentId1));
    }

    private bool IsConnectorUsed(string segmentId, int connectorIndex)
    {
        return Connections.Any(c =>
            (c.Segment1Id == segmentId && c.Segment1ConnectorIndex == connectorIndex) ||
            (c.Segment2Id == segmentId && c.Segment2ConnectorIndex == connectorIndex));
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Called when a segment drag ends. Attempts to snap and connect.
    /// </summary>
    public void OnSegmentDragEnded(TrackSegmentViewModel segment)
    {
        segment.IsDragging = false;
        segment.IsPartOfDragGroup = false;

        // Try to snap to nearest connector
        if (TrySnapToNearestConnector(segment))
        {
            _logger.LogInformation("Segment {ArticleCode} snapped and connected", segment.ArticleCode);
        }
    }

    /// <summary>
    /// Updates segment position during drag (without committing connections).
    /// </summary>
    public void UpdateSegmentPosition(TrackSegmentViewModel segment, double canvasX, double canvasY)
    {
        var newX = canvasX - segment.DragOffsetX;
        var newY = canvasY - segment.DragOffsetY;

        segment.Model.WorldTransform = new Transform2D
        {
            TranslateX = newX,
            TranslateY = newY,
            RotationDegrees = segment.WorldTransform.RotationDegrees
        };

        // Update PathData for visual feedback
        segment.PathData = GeneratePathData(segment.Model);
    }
}