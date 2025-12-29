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
            PathData = "M 0,0 L 239.07,0"
        };

        // G231 - 230.93mm straight
        _templates["G231"] = new TrackGeometry
        {
            ArticleCode = "G231",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(230.93, 0)],
            PathData = "M 0,0 L 230.93,0"
        };

        // G119 - 119.54mm straight
        _templates["G119"] = new TrackGeometry
        {
            ArticleCode = "G119",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(119.54, 0)],
            PathData = "M 0,0 L 119.54,0"
        };

        // G115 - 115.46mm straight
        _templates["G115"] = new TrackGeometry
        {
            ArticleCode = "G115",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(115.46, 0)],
            PathData = "M 0,0 L 115.46,0"
        };

        // G107 - 107.32mm straight (parallel track for K30 crossing)
        _templates["G107"] = new TrackGeometry
        {
            ArticleCode = "G107",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(107.32, 0)],
            PathData = "M 0,0 L 107.32,0"
        };

        // G62 - 61.88mm straight (adaptor track R3-R4)
        _templates["G62"] = new TrackGeometry
        {
            ArticleCode = "G62",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(61.88, 0)],
            PathData = "M 0,0 L 61.88,0"
        };

        // === CURVED TRACKS 30° (Bogengleise) ===
        // Official Piko A-Gleis radii: R1=360mm, R2=421.88mm, R3=483.75mm, R4=545.63mm
        // Arc calculation: x = r * sin(angle), y = r * (1 - cos(angle))
        // For 30° (0.5236 rad): sin(30°)=0.5, cos(30°)=0.866025

        // R1 - Radius 360mm, 30° (12 pieces = full circle)
        // End: x = 360 * 0.5 = 180, y = 360 * (1 - 0.866025) = 48.24
        _templates["R1"] = new TrackGeometry
        {
            ArticleCode = "R1",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(180, 48.24)],
            PathData = "M 0,0 A 360,360 0 0 1 180,48.24"
        };

        // R2 - Radius 421.88mm, 30°
        // End: x = 421.88 * 0.5 = 210.94, y = 421.88 * 0.133975 = 56.52
        _templates["R2"] = new TrackGeometry
        {
            ArticleCode = "R2",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(210.94, 56.52)],
            PathData = "M 0,0 A 421.88,421.88 0 0 1 210.94,56.52"
        };

        // R3 - Radius 483.75mm, 30°
        // End: x = 483.75 * 0.5 = 241.88, y = 483.75 * 0.133975 = 64.80
        _templates["R3"] = new TrackGeometry
        {
            ArticleCode = "R3",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(241.88, 64.80)],
            PathData = "M 0,0 A 483.75,483.75 0 0 1 241.88,64.80"
        };

        // R4 - Radius 545.63mm, 30°
        // End: x = 545.63 * 0.5 = 272.82, y = 545.63 * 0.133975 = 73.08
        _templates["R4"] = new TrackGeometry
        {
            ArticleCode = "R4",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(272.82, 73.08)],
            PathData = "M 0,0 A 545.63,545.63 0 0 1 272.82,73.08"
        };

        // R9 - Radius 907.97mm, 15° (switch turnout radius, 24 pieces = full circle)
        // For 15° (0.2618 rad): sin(15°)=0.25882, cos(15°)=0.96593
        // End: x = 907.97 * 0.25882 = 235.00, y = 907.97 * (1 - 0.96593) = 30.93
        _templates["R9"] = new TrackGeometry
        {
            ArticleCode = "R9",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(235.00, 30.93)],
            PathData = "M 0,0 A 907.97,907.97 0 0 1 235.00,30.93"
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
