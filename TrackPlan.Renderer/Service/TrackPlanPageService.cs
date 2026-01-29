// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer.Service;

using Moba.TrackPlan.Geometry;
using Moba.TrackPlan.Graph;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Example Use Case: TrackPlanPageService
/// Shows how to integrate the Topology-First rendering pipeline into a UI Page.
/// 
/// This service acts as a bridge between the UI and the rendering engine,
/// providing methods to:
/// 1. Load or create track plans from topology
/// 2. Generate layouts
/// 3. Export for visualization
/// 4. Handle user interactions
/// </summary>
public sealed class TrackPlanPageService
{
    private readonly TrackPlanLayoutEngine _layoutEngine;
    private TrackPlanLayout? _currentLayout;
    private TopologyGraph? _currentTopology;

    public TrackPlanPageService(TrackPlanLayoutEngine layoutEngine)
    {
        _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
    }

    /// <summary>
    /// Creates a new track plan from a topology definition.
    /// </summary>
    public bool CreateTrackPlan(TopologyGraph topology, Point2D? startPosition = null, double? startAngle = null)
    {
        ArgumentNullException.ThrowIfNull(topology);

        try
        {
            _currentTopology = topology;
            _currentLayout = _layoutEngine.Process(
                topology,
                startPosition ?? new Point2D(0, 0),
                startAngle ?? 0);

            return _currentLayout.IsValid;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating track plan: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the current layout for rendering.
    /// </summary>
    public TrackPlanLayout? GetCurrentLayout() => _currentLayout;

    /// <summary>
    /// Gets the current topology.
    /// </summary>
    public TopologyGraph? GetCurrentTopology() => _currentTopology;

    /// <summary>
    /// Validates the current layout and returns errors.
    /// </summary>
    public (bool isValid, List<string> errors) ValidateCurrentLayout()
    {
        if (_currentLayout == null)
            return (false, new List<string> { "No layout loaded" });

        var errors = new List<string>();

        foreach (var violation in _currentLayout.ConstraintViolations)
            errors.Add($"Constraint violation: {violation}");

        foreach (var error in _currentLayout.GeometryErrors)
            errors.Add($"Geometry error: {error.Message}");

        return (_currentLayout.IsValid, errors);
    }

    /// <summary>
    /// Exports the current layout to PNG.
    /// </summary>
    public bool ExportToPng(string filePath)
    {
        if (_currentLayout == null)
            return false;

        try
        {
            _layoutEngine.ExportToPng(_currentLayout, filePath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting PNG: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Exports the current layout to SVG.
    /// </summary>
    public string? ExportToSvg()
    {
        if (_currentLayout == null)
            return null;

        try
        {
            return _layoutEngine.ExportToSvg(_currentLayout);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting SVG: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets statistics about the current layout.
    /// </summary>
    public LayoutStatistics? GetStatistics()
    {
        if (_currentLayout == null)
            return null;

        return new LayoutStatistics(
            NodeCount: _currentLayout.Analysis.NodeCount,
            EdgeCount: _currentLayout.Analysis.EdgeCount,
            PrimitiveCount: _currentLayout.Primitives.Count,
            HasCycles: _currentLayout.Analysis.HasCycles,
            ComponentCount: _currentLayout.Analysis.ComponentCount,
            IsValid: _currentLayout.IsValid,
            ConstraintViolationCount: _currentLayout.ConstraintViolations.Count,
            GeometryErrorCount: _currentLayout.GeometryErrors.Count);
    }

    /// <summary>
    /// Gets feedback points for the current layout.
    /// </summary>
    public IEnumerable<FeedbackPointInfo> GetFeedbackPoints()
    {
        return _currentLayout?.FeedbackPoints ?? Enumerable.Empty<FeedbackPointInfo>();
    }

    /// <summary>
    /// Gets signals for the current layout.
    /// </summary>
    public IEnumerable<SignalInfo> GetSignals()
    {
        return _currentLayout?.Signals ?? Enumerable.Empty<SignalInfo>();
    }

    /// <summary>
    /// Resets the current layout.
    /// </summary>
    public void Reset()
    {
        _currentTopology = null;
        _currentLayout = null;
    }
}

/// <summary>
/// Layout statistics for display.
/// </summary>
public sealed record LayoutStatistics(
    int NodeCount,
    int EdgeCount,
    int PrimitiveCount,
    bool HasCycles,
    int ComponentCount,
    bool IsValid,
    int ConstraintViolationCount,
    int GeometryErrorCount);

/// <summary>
/// Example: Complete workflow for creating an R9 Oval Track Plan
/// 
/// This demonstrates the typical workflow:
/// 1. Define topology
/// 2. Create layout
/// 3. Validate
/// 4. Export/Display
/// </summary>
public sealed class R9OvalTrackPlanExample
{
    private readonly TrackPlanPageService _service;

    public R9OvalTrackPlanExample(TrackPlanLayoutEngine layoutEngine)
    {
        _service = new TrackPlanPageService(layoutEngine);
    }

    public void Create()
    {
        // Step 1: Create Topology (24 nodes, 24 edges)
        var topology = BuildR9OvalTopology();

        // Step 2: Create Layout
        var success = _service.CreateTrackPlan(topology);
        if (!success)
        {
            System.Diagnostics.Debug.WriteLine("Failed to create layout");
            return;
        }

        // Step 3: Validate
        var (isValid, errors) = _service.ValidateCurrentLayout();
        System.Diagnostics.Debug.WriteLine($"Layout valid: {isValid}");
        foreach (var error in errors)
            System.Diagnostics.Debug.WriteLine($"  - {error}");

        // Step 4: Display Statistics
        var stats = _service.GetStatistics();
        System.Diagnostics.Debug.WriteLine($"Layout Stats:");
        System.Diagnostics.Debug.WriteLine($"  Nodes: {stats?.NodeCount}");
        System.Diagnostics.Debug.WriteLine($"  Edges: {stats?.EdgeCount}");
        System.Diagnostics.Debug.WriteLine($"  Primitives: {stats?.PrimitiveCount}");
        System.Diagnostics.Debug.WriteLine($"  Has Cycles: {stats?.HasCycles}");

        // Step 5: Export
        _service.ExportToPng("r9_oval.png");
        var svg = _service.ExportToSvg();
        if (svg != null)
        {
            System.IO.File.WriteAllText("r9_oval.svg", svg);
            System.Diagnostics.Debug.WriteLine("Exported to r9_oval.svg");
        }
    }

    private TopologyGraph BuildR9OvalTopology()
    {
        var nodes = new List<TrackNode>();
        for (int i = 0; i < 24; i++)
            nodes.Add(new TrackNode(Guid.NewGuid()));

        var edges = new List<TrackEdge>();

        // Segment 1: 12×R9 (180°)
        for (int i = 0; i < 12; i++)
        {
            var edge = new TrackEdge(Guid.NewGuid(), "R9");
            edge.Connections["A"] = (nodes[i].Id, null, null);
            edge.Connections["B"] = (nodes[i + 1].Id, null, null);
            edges.Add(edge);
        }

        // Segment 2: 1×WR (15°)
        var wrEdge = new TrackEdge(Guid.NewGuid(), "WR");
        wrEdge.Connections["A"] = (nodes[12].Id, null, null);
        wrEdge.Connections["B"] = (nodes[13].Id, null, null);
        edges.Add(wrEdge);

        // Segment 3: 11×R9 (165°)
        for (int i = 0; i < 11; i++)
        {
            int fromNodeIdx = 13 + i;
            int toNodeIdx = (13 + i + 1) % 24;

            var edge = new TrackEdge(Guid.NewGuid(), "R9");
            edge.Connections["A"] = (nodes[fromNodeIdx].Id, null, null);
            edge.Connections["B"] = (nodes[toNodeIdx].Id, null, null);
            edges.Add(edge);
        }

        // Build and return graph
        var graph = new TopologyGraph
        {
            Nodes = nodes,
            Edges = edges
        };

        return graph;
    }
}
