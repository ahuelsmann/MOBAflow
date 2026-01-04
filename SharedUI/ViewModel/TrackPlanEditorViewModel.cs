// Copyright ...

namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Interface;

using Microsoft.Extensions.Logging;

using Service;
using Moba.TrackPlan.Domain;
using TrackPlan.Geometry;
using TrackPlan.Import.AnyRail;
using TrackPlan.Renderer;
using Moba.TrackPlan.Service;

using System.Collections.ObjectModel;

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
    private readonly ConnectorMatcher _connectorMatcher;

    // ⭐ NEW: TopologySolver mit eigenem Logger
    private readonly TopologySolver _topologySolver;

    // --------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------

    public TrackPlanEditorViewModel(
        MainWindowViewModel mainViewModel,
        IIoService ioService,
        ILogger<TrackPlanEditorViewModel> logger,
        ILogger<ConnectorMatcher> matcherLogger,
        ILogger<TopologySolver> topologyLogger)   // ⭐ NEW
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;
        _logger = logger;

        _geometryLibrary = new TrackGeometryLibrary();
        _renderer = new TopologyRenderer(_geometryLibrary);
        _feedbackStateManager = new FeedbackStateManager();
        _connectorMatcher = new ConnectorMatcher(_geometryLibrary, matcherLogger);

        // ⭐ NEW: TopologySolver korrekt initialisieren
        _topologySolver = new TopologySolver(_geometryLibrary, topologyLogger);

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

    // --------------------------------------------------------------------
    // Track Library
    // --------------------------------------------------------------------

    public ObservableCollection<TrackTemplateViewModel> TrackLibrary { get; } = [];

    [ObservableProperty]
    private TrackTemplateViewModel? selectedTemplate;

    private void LoadTrackLibrary()
    {
        TrackLibrary.Clear();
        foreach (var template in PikoATrackLibrary.Templates)
            TrackLibrary.Add(new TrackTemplateViewModel(template));
    }

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

    [RelayCommand]
    private void AddSegment(TrackTemplateViewModel? template)
    {
        if (template == null) return;

        var segment = new TrackSegmentModel
        {
            Id = Guid.NewGuid().ToString(),
            ArticleCode = template.ArticleCode,
            WorldTransform = Transform2D.Identity
        };

        var vm = new TrackSegmentViewModel(segment);
        Segments.Add(vm);
        SelectedSegment = vm;

        RenderLayout();
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
        // ⭐ NEW: Topologie lösen → WorldTransforms setzen
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

    private string GeneratePathData(TrackSegmentModel segment)
    {
        var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
        return geometry?.PathData ?? "M 0,0";
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

    // --------------------------------------------------------------------
    // Persistence
    // --------------------------------------------------------------------

    private void SyncToProject()
    {
        var project = _mainViewModel.SelectedProject?.Model;
        if (project == null) return;

        var layout = project.TrackLayout ??= new TrackLayoutModel();
        layout.Name = LayoutName;
        layout.Description = LayoutDescription;
        layout.Segments = Segments.Select(s => s.Model).ToList();
        layout.Connections = Connections.ToList();
    }

    private void LoadFromProject()
    {
        var layout = _mainViewModel.SelectedProject?.Model.TrackLayout;
        if (layout is not TrackLayoutModel trackLayout)
            return;

        Segments.Clear();
        Connections.Clear();

        LayoutName = trackLayout.Name;
        LayoutDescription = trackLayout.Description;

        foreach (var segment in trackLayout.Segments)
            Segments.Add(new TrackSegmentViewModel(segment));

        Connections.AddRange(trackLayout.Connections);

        RenderLayout();
    }

    // --------------------------------------------------------------------
    // Feedback State
    // --------------------------------------------------------------------

    private void OnFeedbackStateChanged(object? sender, FeedbackStateChangedEventArgs e)
    {
        var segment = Segments.FirstOrDefault(s => s.Id == e.SegmentId);
        if (segment != null)
        {
            var stateText = e.IsOccupied ? "OCCUPIED" : "FREE";
            _logger.LogInformation(
                "Segment {SegmentId} ({ArticleCode}) InPort {InPort} is now {State}",
                e.SegmentId, e.ArticleCode, e.InPort, stateText);
        }
    }

    [RelayCommand]
    private async Task ImportFromAnyRailXml()
    {
        var file = await _ioService.BrowseForXmlFileAsync();
        if (file == null) return;

        var anyRailLayout = await AnyRailLayout.ParseAsync(file);

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
}