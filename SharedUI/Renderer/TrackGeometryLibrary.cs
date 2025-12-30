// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System;
using System.Collections.Generic;

namespace Moba.SharedUI.Renderer;

/// <summary>
/// Runtime track template library for the renderer.
/// Maps ArticleCodes to TrackGeometry (Endpoints + PathData).
/// Based on official Piko A-Gleis dimensions from track catalog.
/// </summary>
public class TrackGeometryLibrary
{
    private readonly Dictionary<string, TrackGeometry> _templates = new();

    public TrackGeometryLibrary()
    {
        InitializeTemplates();
    }

    private void InitializeTemplates()
    {
        // === STRAIGHT TRACKS (Gerade Gleise) ===
        // Official Piko A-Gleis dimensions from track catalog
        
        // G239 - 239.07mm straight
        _templates["G239"] = new TrackGeometry
        {
            ArticleCode = "G239",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(239.07, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 239.07,0"
        };

        // G231 - 230.93mm straight
        _templates["G231"] = new TrackGeometry
        {
            ArticleCode = "G231",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(230.93, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 230.93,0"
        };

        // G119 - 119.54mm straight
        _templates["G119"] = new TrackGeometry
        {
            ArticleCode = "G119",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(119.54, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 119.54,0"
        };

        // G115 - 115.46mm straight
        _templates["G115"] = new TrackGeometry
        {
            ArticleCode = "G115",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(115.46, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 115.46,0"
        };

        // G107 - 107.32mm straight (parallel track for K30 crossing)
        _templates["G107"] = new TrackGeometry
        {
            ArticleCode = "G107",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(107.32, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 107.32,0"
        };

        // G62 - 61.88mm straight (adaptor track R3-R4)
        _templates["G62"] = new TrackGeometry
        {
            ArticleCode = "G62",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(61.88, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 61.88,0"
        };

        // === CURVED TRACKS 30° (Bogengleise) ===
        // Official Piko A-Gleis radii: R1=360mm, R2=421.88mm, R3=483.75mm, R4=545.63mm
        // Arc calculation: x = r * sin(angle), y = r * (1 - cos(angle))
        // For 30° (0.5236 rad): sin(30°)=0.5, cos(30°)=0.866025

        // R1 - Radius 360mm, 30° (12 pieces = full circle)
        // End: x = 360 * 0.5 = 180, y = 360 * (1 - 0.866025) = 48.24
        // Headings: 0° at start (horizontal right), 30° at end (tangent direction)
        _templates["R1"] = new TrackGeometry
        {
            ArticleCode = "R1",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(180, 48.24)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 360,360 0 0 1 180,48.24"
        };

        // R2 - Radius 421.88mm, 30°
        // End: x = 421.88 * 0.5 = 210.94, y = 421.88 * 0.133975 = 56.52
        _templates["R2"] = new TrackGeometry
        {
            ArticleCode = "R2",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(210.94, 56.52)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 421.88,421.88 0 0 1 210.94,56.52"
        };

        // R3 - Radius 483.75mm, 30°
        // End: x = 483.75 * 0.5 = 241.88, y = 483.75 * 0.133975 = 64.80
        _templates["R3"] = new TrackGeometry
        {
            ArticleCode = "R3",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(241.88, 64.80)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 483.75,483.75 0 0 1 241.88,64.80"
        };

        // R4 - Radius 545.63mm, 30°
        // End: x = 545.63 * 0.5 = 272.82, y = 545.63 * 0.133975 = 73.08
        _templates["R4"] = new TrackGeometry
        {
            ArticleCode = "R4",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(272.82, 73.08)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 545.63,545.63 0 0 1 272.82,73.08"
        };

        // R9 - Radius 907.97mm, 15° (switch turnout radius, 24 pieces = full circle)
        // For 15° (0.2618 rad): sin(15°)=0.25882, cos(15°)=0.96593
        // End: x = 907.97 * 0.25882 = 235.00, y = 907.97 * (1 - 0.96593) = 30.93
        _templates["R9"] = new TrackGeometry
        {
            ArticleCode = "R9",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(235.00, 30.93)],
            EndpointHeadingsDeg = [0, 195],
            PathData = "M 0,0 A 907.97,907.97 0 0 1 235.00,30.93"
        };

        // === TURNOUTS (Weichen) ===
        // Based on official Piko A-Gleis H0 specifications (Catalog 55420-55427)
        // All turnouts: Base length G231 (230.93mm), R9 curve (907.97mm radius, 15°)
        
        // WR - Right Regular Turnout (Rechtsweiche) - Piko 55420
        // Length: 230.93mm (G231), Diverging curve: R9 (907.97mm), 15° angle
        // Calculation: x = r*sin(15°) = 907.97*0.25882 = 235.00
        //              y = r*(1-cos(15°)) = 907.97*0.03407 = 30.93
        // 3 endpoints: [0] entry, [1] straight exit, [2] diverging exit (right)
        _templates["WR"] = new TrackGeometry
        {
            ArticleCode = "WR",
            Endpoints = [
                new TrackPoint(0, 0),          // Entry point
                new TrackPoint(230.93, 0),     // Straight through exit
                new TrackPoint(235.00, 30.93)  // Diverging exit (R9, 15°)
            ],
            EndpointHeadingsDeg = [0, 180, 195], // Entry 0°, Straight 180°, Diverging 195° (0° + 180° + 15°)
            PathData = "M 0,0 L 230.93,0 M 0,0 A 907.97,907.97 0 0 1 235.00,30.93"
        };

        // WL - Left Regular Turnout (Linksweiche) - Piko 55421
        // Same as WR but mirrored (y-axis negated for diverging track)
        // 3 endpoints: [0] entry, [1] straight exit, [2] diverging exit (left)
        _templates["WL"] = new TrackGeometry
        {
            ArticleCode = "WL",
            Endpoints = [
                new TrackPoint(0, 0),           // Entry point
                new TrackPoint(230.93, 0),      // Straight through exit
                new TrackPoint(235.00, -30.93)  // Diverging exit (R9, -15°, mirrored)
            ],
            EndpointHeadingsDeg = [0, 180, 165], // Entry 0°, Straight 180°, Diverging 165° (180° - 15°)
            PathData = "M 0,0 L 230.93,0 M 0,0 A 907.97,907.97 0 0 0 235.00,-30.93"
        };

        // W3 - Three-way Turnout (Dreiwegweiche) - Piko 55424
        // Combines both WL and WR diverging tracks from a single entry
        // 4 endpoints: [0] entry, [1] straight, [2] diverging right, [3] diverging left
        _templates["W3"] = new TrackGeometry
        {
            ArticleCode = "W3",
            Endpoints = [
                new TrackPoint(0, 0),           // Entry point
                new TrackPoint(230.93, 0),      // Straight through exit
                new TrackPoint(235.00, 30.93),   // Diverging right (R9, 15°)
                new TrackPoint(235.00, -30.93)   // Diverging left (R9, -15°)
            ],
            EndpointHeadingsDeg = [0, 180, 195, 165],
            PathData = "M 0,0 L 230.93,0 M 0,0 A 907.97,907.97 0 0 1 235.00,30.93 M 0,0 A 907.97,907.97 0 0 0 235.00,-30.93"
        };

        // DKW - Double Slip Switch (Doppelte Kreuzungsweiche) - Piko 55427
        // Two crossing tracks, both can switch. Complex geometry with 4 endpoints.
        // Endpoints form a parallelogram: straight track + diagonal offset track
        // Using 15° crossing angle, length G231
        // 4 endpoints: [0] entry A, [1] exit A, [2] entry B, [3] exit B
        _templates["DKW"] = new TrackGeometry
        {
            ArticleCode = "DKW",
            Endpoints = [
                new TrackPoint(0, 0),            // Entry A (main track)
                new TrackPoint(230.93, 0),       // Exit A (main track through)
                new TrackPoint(59.70, 15.47),    // Entry B (crossing track, offset by 15° angle)
                new TrackPoint(290.63, 15.47)    // Exit B (crossing track exit)
            ],
            EndpointHeadingsDeg = [0, 180, 15, 195], // A: 0°/180°, B: 15°/195° (15° crossing)
            PathData = "M 0,0 L 230.93,0 M 59.70,15.47 L 290.63,15.47"
        };
    }

    /// <summary>
    /// Get track geometry by article code.
    /// </summary>
    public TrackGeometry? GetGeometry(string articleCode)
    {
        return _templates.TryGetValue(articleCode, out var geom) ? geom : null;
    }

    /// <summary>
    /// Get all available article codes.
    /// </summary>
    public IEnumerable<string> GetArticleCodes() => _templates.Keys;
}

/// <summary>
/// Track geometry with endpoints and SVG path data.
/// </summary>
public class TrackGeometry
{
    public string ArticleCode { get; set; } = string.Empty;
    public List<TrackPoint> Endpoints { get; set; } = new();
    /// <summary>
    /// Heading (in degrees) for each endpoint in local coordinates.
    /// 0° = +X direction, 90° = +Y direction.
    /// The count should match <see cref="Endpoints"/>.
    /// Used by the topology renderer to align segments by rotation.
    /// </summary>
    public List<double> EndpointHeadingsDeg { get; set; } = new();
    public string PathData { get; set; } = string.Empty;
}

/// <summary>
/// 2D point in millimeters (track coordinate system).
/// </summary>
public class TrackPoint
{
    public double X { get; set; }
    public double Y { get; set; }

    public TrackPoint() { }
    public TrackPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}
