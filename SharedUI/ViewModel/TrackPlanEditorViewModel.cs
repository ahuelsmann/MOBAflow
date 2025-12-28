// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.TrackPlan;
using Interface;
using Service;
using System.Collections.ObjectModel;
using System.Diagnostics;

/// <summary>
/// ViewModel for TrackPlanEditorPage - topology-based track plan editor.
/// Positions and paths are calculated by TrackLayoutRenderer from the connection graph.
/// </summary>
public partial class TrackPlanEditorViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly IIoService _ioService;
    private readonly TrackLayoutRenderer _renderer = new();

    public TrackPlanEditorViewModel(MainWindowViewModel mainViewModel, IIoService ioService)
    {
        _mainViewModel = mainViewModel;
        _ioService = ioService;

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

    #region Zoom
    /// <summary>
    /// Zoom level in percent (25-200%). Default 100% shows the layout at calculated scale.
    /// </summary>
    [ObservableProperty]
    private double zoomLevel = 100.0;

    /// <summary>
    /// Zoom factor for ScaleTransform (0.25 - 2.0).
    /// </summary>
    public double ZoomFactor => ZoomLevel / 100.0;

    /// <summary>
    /// Display text for zoom level.
    /// </summary>
    public string ZoomLevelText => $"{ZoomLevel:F0}%";

    partial void OnZoomLevelChanged(double value)
    {
        _ = value;
        OnPropertyChanged(nameof(ZoomFactor));
        OnPropertyChanged(nameof(ZoomLevelText));
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(200, ZoomLevel + 5);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(25, ZoomLevel - 5);
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

        // Calculate positions and path data
        var rendered = _renderer.Render(layout);

        // Apply to ViewModels
        foreach (var rs in rendered)
        {
            var vm = Segments.FirstOrDefault(s => s.Id == rs.Id);
            if (vm != null)
            {
                vm.X = rs.X;
                vm.Y = rs.Y;
                vm.PathData = rs.PathData;
            }
        }

        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(StatusText));
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
        var layout = _mainViewModel.SelectedProject?.Model?.TrackLayout;
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

    [RelayCommand]
    private async Task ImportAnyRailAsync()
    {
        var file = await _ioService.BrowseForXmlFileAsync();
        if (file == null) return;

        var anyRailLayout = await AnyRailLayout.ParseAsync(file);

        Segments.Clear();
        Connections.Clear();

        // Build endpoint lookup
        var endpointLookup = anyRailLayout.Endpoints.ToDictionary(e => e.Nr);

        // Calculate scale and offset to fit layout in view
        var (scale, offsetX, offsetY) = CalculateScaleAndOffset(anyRailLayout, endpointLookup);

        // Convert AnyRail parts to segments with scaled endpoint coordinates
        foreach (var part in anyRailLayout.Parts)
        {
            var articleCode = part.GetArticleCode();
            
            // Get endpoint coordinates from AnyRail XML, scaled and offset
            var endpoints = new List<SegmentEndpoint>();
            foreach (var epNr in part.EndpointNrs)
            {
                if (endpointLookup.TryGetValue(epNr, out var ep))
                {
                    endpoints.Add(new SegmentEndpoint
                    {
                        X = ep.X * scale + offsetX,
                        Y = ep.Y * scale + offsetY
                    });
                }
            }
            
            // Convert AnyRail lines to domain DrawingLines
            var lines = part.Lines.Select(line => new DrawingLine
            {
                X1 = line.Pt1.X * scale + offsetX,
                Y1 = line.Pt1.Y * scale + offsetY,
                X2 = line.Pt2.X * scale + offsetX,
                Y2 = line.Pt2.Y * scale + offsetY
            }).ToList();
            
            // Convert AnyRail arcs to domain DrawingArcs
            var arcs = part.Arcs.Select(arc => new DrawingArc
            {
                X1 = arc.Pt1.X * scale + offsetX,
                Y1 = arc.Pt1.Y * scale + offsetY,
                X2 = arc.Pt2.X * scale + offsetX,
                Y2 = arc.Pt2.Y * scale + offsetY,
                Radius = arc.Radius * scale,
                Sweep = 0  // Default sweep, will be calculated if needed
            }).ToList();
            
            var segment = new TrackSegment
            {
                Id = part.Id,
                ArticleCode = articleCode,
                Endpoints = endpoints,
                Lines = lines,
                Arcs = arcs
            };
            
            Segments.Add(new TrackSegmentViewModel(segment));
        }

        // Get connections directly from XML
        Connections.AddRange(anyRailLayout.ToTrackConnections());

        // Render layout from endpoints
        RenderLayout();

        Debug.WriteLine($"📂 Imported AnyRail: {Segments.Count} segments, {Connections.Count} connections (scale: {scale:F3})");
    }

    /// <summary>
    /// Calculate scale and offset to center layout in view.
    /// </summary>
    private static (double scale, double offsetX, double offsetY) CalculateScaleAndOffset(
        AnyRailLayout layout, Dictionary<int, AnyRailEndpoint> endpointLookup)
    {
        if (layout.Parts.Count == 0)
            return (0.15, 50, 50);

        // Find bounding box from all endpoints
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var part in layout.Parts)
        {
            foreach (var epNr in part.EndpointNrs)
            {
                if (endpointLookup.TryGetValue(epNr, out var ep))
                {
                    minX = Math.Min(minX, ep.X);
                    minY = Math.Min(minY, ep.Y);
                    maxX = Math.Max(maxX, ep.X);
                    maxY = Math.Max(maxY, ep.Y);
                }
            }
        }

        var layoutWidth = maxX - minX;
        var layoutHeight = maxY - minY;

        // Target view size (Canvas dimensions)
        const double targetWidth = 1000.0;
        const double targetHeight = 600.0;
        const double padding = 50.0;

        var availableWidth = targetWidth - 2 * padding;
        var availableHeight = targetHeight - 2 * padding;

        // Calculate scale to fit
        var scaleX = availableWidth / layoutWidth;
        var scaleY = availableHeight / layoutHeight;
        var scale = Math.Min(scaleX, scaleY);
        scale = Math.Clamp(scale, 0.05, 0.5);

        // Calculate scaled layout dimensions
        var scaledWidth = layoutWidth * scale;
        var scaledHeight = layoutHeight * scale;

        // Calculate offset to CENTER the layout in the view
        var offsetX = (targetWidth - scaledWidth) / 2 - (minX * scale);
        var offsetY = (targetHeight - scaledHeight) / 2 - (minY * scale);

        return (scale, offsetX, offsetY);
    }

    #endregion
}
