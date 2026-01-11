// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.ViewModel;

using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Editor.Command;
using Moba.TrackPlan.Editor.Service;
using Moba.TrackPlan.Editor.ViewState;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.Layout;
using Moba.TrackPlan.Renderer.World;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// ViewModel for TrackPlanEditorPage2 - uses new TopologyGraph-based architecture.
/// </summary>
public sealed class TrackPlanEditorViewModel2
{
    private readonly TrackGeometryRenderer _renderer;
    private readonly ILayoutEngine _layoutEngine;
    private readonly ValidationService _validationService;
    private readonly SerializationService _serializationService;
    private readonly IEnumerable<ITopologyConstraint> _constraints;

    public TopologyGraph Graph { get; }

    public EditorViewState ViewState { get; } = new();
    public SelectionState Selection { get; } = new();
    public VisibilityState Visibility { get; } = new();

    public AssignFeedbackPointCommand AssignFeedbackPointCommand { get; }
    public RemoveFeedbackPointCommand RemoveFeedbackPointCommand { get; }

    public SelectionController SelectionController { get; }
    public FeedbackPointController FeedbackPointController { get; }

    public TrackPlanEditorViewModel2(
        TrackGeometryRenderer renderer,
        ILayoutEngine layoutEngine,
        ValidationService validationService,
        SerializationService serializationService,
        IEnumerable<ITopologyConstraint> constraints)
    {
        _renderer = renderer;
        _layoutEngine = layoutEngine;
        _validationService = validationService;
        _serializationService = serializationService;
        _constraints = constraints;

        // Create empty graph and add constraints
        Graph = new TopologyGraph();
        foreach (var constraint in _constraints)
        {
            Graph.Constraints.Add(constraint);
        }

        // Initialize commands
        AssignFeedbackPointCommand = new AssignFeedbackPointCommand(Graph);
        RemoveFeedbackPointCommand = new RemoveFeedbackPointCommand(Graph);

        // Initialize controllers
        SelectionController = new SelectionController(Selection);
        FeedbackPointController = new FeedbackPointController(ViewState);
    }

    public Dictionary<Guid, Point2D> CalculateLayout()
        => _layoutEngine.Layout(Graph);

    public IEnumerable<IGeometryPrimitive> RenderEdge(TrackEdge edge, TrackTemplate template, Point2D start, Point2D end)
        => _renderer.Render(edge, template, start, end);

    public IReadOnlyList<ConstraintViolation> Validate()
        => _validationService.Validate(Graph);

    public string Serialize()
        => _serializationService.Serialize(Graph);

    public void LoadFromJson(string json)
    {
        var loadedGraph = _serializationService.Deserialize(json);

        Graph.Nodes.Clear();
        Graph.Edges.Clear();
        Graph.Endcaps.Clear();

        foreach (var node in loadedGraph.Nodes)
            Graph.Nodes.Add(node);

        foreach (var edge in loadedGraph.Edges)
            Graph.Edges.Add(edge);

        foreach (var endcap in loadedGraph.Endcaps)
            Graph.Endcaps.Add(endcap);
    }

    public void AddNode(TrackNode node)
        => Graph.Nodes.Add(node);

    public void AddEdge(TrackEdge edge)
        => Graph.Edges.Add(edge);

    public void RemoveNode(Guid nodeId)
    {
        var node = Graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
            Graph.Nodes.Remove(node);
    }

    public void RemoveEdge(Guid edgeId)
    {
        var edge = Graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge != null)
            Graph.Edges.Remove(edge);
    }

    public void Clear()
    {
        Graph.Nodes.Clear();
        Graph.Edges.Clear();
        Graph.Endcaps.Clear();
        ClearSelection();
    }

    public void ClearSelection()
        => SelectionController.Clear();

    public void SelectEdge(Guid edgeId)
        => SelectionController.SelectSingle(edgeId);

    public void ToggleEdgeSelection(Guid edgeId)
        => SelectionController.Toggle(edgeId);
}
