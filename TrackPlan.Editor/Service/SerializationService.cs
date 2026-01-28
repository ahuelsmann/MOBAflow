// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackPlan.Graph;
using Moba.TrackPlan.Editor.ViewModel;

using System.Text.Json;

public sealed class SerializationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string Serialize(TopologyGraph graph)
        => JsonSerializer.Serialize(graph, JsonOptions);

    public TopologyGraph Deserialize(string json)
        => JsonSerializer.Deserialize<TopologyGraph>(json)
           ?? throw new InvalidOperationException("Invalid graph JSON.");

    /// <summary>
    /// Serializes a complete track plan (graph + positions + rotations) to JSON.
    /// </summary>
    public string SerializeTrackPlan(
        TrackPlanEditorViewModel viewModel,
        string name = "Untitled",
        string description = "",
        string catalogId = "PikoA")
    {
        var trackPlanData = new
        {
            id = Guid.NewGuid().ToString(),
            name,
            description,
            catalogId,
            nodes = viewModel.Graph.Nodes.Select(n => new
            {
                id = n.Id.ToString(),
                ports = n.Ports.ToList()
            }).ToList(),
            edges = viewModel.Graph.Edges.Select(e => new
            {
                id = e.Id.ToString(),
                templateId = e.TemplateId,
                rotationDeg = e.RotationDeg,
                startPortId = e.StartPortId,
                endPortId = e.EndPortId,
                startNodeId = e.StartNodeId?.ToString(),
                endNodeId = e.EndNodeId?.ToString(),
                feedbackPointNumber = e.FeedbackPointNumber,
                connections = e.Connections
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            nodeId = kvp.Value.NodeId.ToString(),
                            connectedEdgeId = kvp.Value.ConnectedEdgeId?.ToString(),
                            connectedPortId = kvp.Value.ConnectedPortId
                        })
            }).ToList(),
            positions = viewModel.Positions
                .ToDictionary(kvp => kvp.Key.ToString(), kvp => new { x = kvp.Value.X, y = kvp.Value.Y }),
            rotations = viewModel.Rotations
                .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            sections = viewModel.Sections.Select(s => new
            {
                id = s.Id.ToString(),
                name = s.Name,
                color = s.Color,
                trackIds = s.TrackIds.Select(t => t.ToString()).ToList()
            }).ToList(),
            isolators = viewModel.Isolators.Select(i => new
            {
                id = i.Id.ToString(),
                edgeId = i.EdgeId.ToString(),
                portId = i.PortId
            }).ToList(),
            endcaps = viewModel.Endcaps.Select(e => new
            {
                id = e.Id.ToString(),
                kind = e.Kind.ToString(),
                attachedTo = new
                {
                    nodeId = e.AttachedTo.NodeId.ToString(),
                    portId = e.AttachedTo.PortId
                }
            }).ToList(),
            lastModified = DateTime.UtcNow.ToString("O")
        };

        return JsonSerializer.Serialize(trackPlanData, JsonOptions);
    }

    /// <summary>
    /// Deserializes a track plan from JSON and applies it to the view model.
    /// </summary>
    public void DeserializeTrackPlan(TrackPlanEditorViewModel viewModel, string json)
    {
        var trackPlanJson = JsonDocument.Parse(json);
        var root = trackPlanJson.RootElement;

        // Clear current state
        viewModel.Graph.Nodes.Clear();
        viewModel.Graph.Edges.Clear();
        viewModel.Positions.Clear();
        viewModel.Rotations.Clear();
        viewModel.Sections.Clear();
        viewModel.Isolators.Clear();
        viewModel.Endcaps.Clear();
        viewModel.ClearSelection();

        // Restore nodes
        if (root.TryGetProperty("nodes", out var nodesElement))
        {
            foreach (var nodeElement in nodesElement.EnumerateArray())
            {
                if (Guid.TryParse(nodeElement.GetProperty("id").GetString(), out var nodeId))
                {
                    var n = new TrackNode(nodeId);
                    if (nodeElement.TryGetProperty("ports", out var portsElement))
                    {
                        foreach (var portElement in portsElement.EnumerateArray())
                        {
                            n.Ports.Add(portElement.GetString() ?? string.Empty);
                        }
                    }
                    viewModel.Graph.Nodes.Add(n);
                }
            }
        }

        // Restore edges
        if (root.TryGetProperty("edges", out var edgesElement))
        {
            foreach (var edgeElement in edgesElement.EnumerateArray())
            {
                if (Guid.TryParse(edgeElement.GetProperty("id").GetString(), out var edgeId))
                {
                    var templateId = edgeElement.GetProperty("templateId").GetString() ?? string.Empty;
                    var e = new TrackEdge(edgeId, templateId)
                    {
                        RotationDeg = edgeElement.GetProperty("rotationDeg").GetDouble(),
                        StartPortId = edgeElement.GetProperty("startPortId").GetString() ?? string.Empty,
                        EndPortId = edgeElement.GetProperty("endPortId").GetString() ?? string.Empty
                    };

                    if (edgeElement.TryGetProperty("startNodeId", out var startNodeIdElement) &&
                        Guid.TryParse(startNodeIdElement.GetString(), out var startNodeId))
                        e.StartNodeId = startNodeId;

                    if (edgeElement.TryGetProperty("endNodeId", out var endNodeIdElement) &&
                        Guid.TryParse(endNodeIdElement.GetString(), out var endNodeId))
                        e.EndNodeId = endNodeId;

                    if (edgeElement.TryGetProperty("feedbackPointNumber", out var fbElement))
                        e.FeedbackPointNumber = fbElement.GetInt32();

                    viewModel.Graph.Edges.Add(e);
                }
            }
        }

        // Restore positions
        if (root.TryGetProperty("positions", out var positionsElement))
        {
            foreach (var prop in positionsElement.EnumerateObject())
            {
                if (Guid.TryParse(prop.Name, out var guid))
                {
                    var x = prop.Value.GetProperty("x").GetDouble();
                    var y = prop.Value.GetProperty("y").GetDouble();
                    viewModel.Positions[guid] = new Point2D(x, y);
                }
            }
        }

        // Restore rotations
        if (root.TryGetProperty("rotations", out var rotationsElement))
        {
            foreach (var prop in rotationsElement.EnumerateObject())
            {
                if (Guid.TryParse(prop.Name, out var guid))
                {
                    viewModel.Rotations[guid] = prop.Value.GetDouble();
                }
            }
        }
    }
}