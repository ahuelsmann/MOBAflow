// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System;
using System.Collections.Generic;
using Moba.TrackPlan.Domain;

namespace Moba.TrackPlan.Renderer;

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
        // ============================================================================
        // OFFICIAL PIKO A-GLEIS GEOMETRY CATALOG
        // Source: Piko official track catalog (2025)
        // Parallelkreisabstand: 61.88mm between R1↔R2, R2↔R3, R3↔R4
        // ============================================================================
        //
        // ENDPOINT COUNTS (CRITICAL for ConnectorMatcher):
        //  - Straight/Curves (G231, G119, G62, R1-R4, R9): 2 Endpoints
        //  - Simple Turnouts (WL, WR): 3 Endpoints (Entry, Straight, Diverging)
        //  - Curved Switches (BWL, BWR, BWL-R3, BWR-R3): 3 Endpoints (Entry, Inner Exit, Outer Exit)
        //  - Y-Switch (WY): 3 Endpoints (Entry, Right Branch, Left Branch)
        //  - Three-way Turnout (W3): 4 Endpoints (Entry, Straight, Right, Left)
        //  - Double Slip (DKW): 4 Endpoints (Entry A, Exit A, Entry B, Exit B)
        //  - Crossings (K15, K30): 4 Endpoints (Entry A, Exit A, Entry B, Exit B)
        // ============================================================================

        // === STRAIGHT TRACKS (Gerade Gleise) ===
        
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

        // G62 - 61.88mm straight (adaptor track R3-R4, parallel track spacing)
        _templates["G62"] = new TrackGeometry
        {
            ArticleCode = "G62",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(61.88, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 61.88,0"
        };

        // G940 - 940mm flexible track (BS-G940)
        _templates["G940"] = new TrackGeometry
        {
            ArticleCode = "G940",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(940, 0)],
            EndpointHeadingsDeg = [0, 180],
            PathData = "M 0,0 L 940,0"
        };

        // === CURVED TRACKS 30° (Bogengleise) ===
        // Official Piko A-Gleis radii with 61.88mm parallel spacing
        // Arc calculation: x = r * sin(30°), y = r * (1 - cos(30°))
        // For 30° (0.5236 rad): sin(30°)=0.5, cos(30°)=0.866025
        // 12 pieces = full circle (360°)

        // R1 - Radius 360.00mm, 30°
        // End: x = 360 * 0.5 = 180.00, y = 360 * 0.133975 = 48.23
        _templates["R1"] = new TrackGeometry
        {
            ArticleCode = "R1",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(180.00, 48.23)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 360.00,360.00 0 0 1 180.00,48.23"
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

        // R9 - Radius 907.97mm, 15° (switch turnout radius, Weichengegenbogen)
        // For 15° (0.2618 rad): sin(15°)=0.25882, cos(15°)=0.96593
        // End: x = 907.97 * 0.25882 = 235.00, y = 907.97 * 0.03407 = 30.93
        // 24 pieces = full circle (360°)
        _templates["R9"] = new TrackGeometry
        {
            ArticleCode = "R9",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(235.00, 30.93)],
            EndpointHeadingsDeg = [0, 15],
            PathData = "M 0,0 A 907.97,907.97 0 0 1 235.00,30.93"
        };

        // === TURNOUTS / SWITCHES (Weichen) ===
        // All turnouts: Base length G231 (230.93mm), R9 curve (907.97mm radius, 15°)
        
        // WR / (BS-)WR - Right Regular Turnout (Rechtsweiche) - Piko 55420
        _templates["WR"] = new TrackGeometry
        {
            ArticleCode = "WR",
            Endpoints = [
                new TrackPoint(0, 0),          // [0] Entry point
                new TrackPoint(230.93, 0),     // [1] Straight through exit
                new TrackPoint(235.00, 30.93)  // [2] Diverging exit (R9, 15° right)
            ],
            EndpointHeadingsDeg = [0, 180, 195], // Entry 0°, Straight 180°, Diverging 195°
            PathData = "M 0,0 L 230.93,0 M 0,0 A 907.97,907.97 0 0 1 235.00,30.93"
        };

        // WL / (BS-)WL - Left Regular Turnout (Linksweiche) - Piko 55421
        _templates["WL"] = new TrackGeometry
        {
            ArticleCode = "WL",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry point
                new TrackPoint(230.93, 0),      // [1] Straight through exit
                new TrackPoint(235.00, -30.93)  // [2] Diverging exit (R9, 15° left)
            ],
            EndpointHeadingsDeg = [0, 180, 165], // Entry 0°, Straight 180°, Diverging 165°
            PathData = "M 0,0 L 230.93,0 M 0,0 A 907.97,907.97 0 0 0 235.00,-30.93"
        };

        // W3 - Three-way Turnout (Dreiwegweiche) - Piko 55424
        _templates["W3"] = new TrackGeometry
        {
            ArticleCode = "W3",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry point
                new TrackPoint(230.93, 0),      // [1] Straight through exit
                new TrackPoint(235.00, 30.93),  // [2] Diverging right (R9, 15°)
                new TrackPoint(235.00, -30.93)  // [3] Diverging left (R9, 15°)
            ],
            EndpointHeadingsDeg = [0, 180, 195, 165],
            PathData = "M 0,0 L 230.93,0 M 0,0 A 907.97,907.97 0 0 1 235.00,30.93 M 0,0 A 907.97,907.97 0 0 0 235.00,-30.93"
        };

        // WY - Y-Switch (Y-Weiche)
        // Symmetrical split (both branches at ±15° from center)
        _templates["WY"] = new TrackGeometry
        {
            ArticleCode = "WY",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry point
                new TrackPoint(235.00, 30.93),  // [1] Right branch (R9, 15°)
                new TrackPoint(235.00, -30.93)  // [2] Left branch (R9, 15°)
            ],
            EndpointHeadingsDeg = [0, 195, 165], // Entry 0°, Right 195°, Left 165°
            PathData = "M 0,0 A 907.97,907.97 0 0 1 235.00,30.93 M 0,0 A 907.97,907.97 0 0 0 235.00,-30.93"
        };

        // DKW - Double Slip Switch (Doppelte Kreuzungsweiche) - Piko 55427
        _templates["DKW"] = new TrackGeometry
        {
            ArticleCode = "DKW",
            Endpoints = [
                new TrackPoint(0, 0),            // [0] Entry A (main track)
                new TrackPoint(230.93, 0),       // [1] Exit A (main track through)
                new TrackPoint(59.70, 15.47),    // [2] Entry B (crossing track, offset by 15°)
                new TrackPoint(290.63, 15.47)    // [3] Exit B (crossing track exit)
            ],
            EndpointHeadingsDeg = [0, 180, 15, 195],
            PathData = "M 0,0 L 230.93,0 M 59.70,15.47 L 290.63,15.47"
        };

        // === CURVED SWITCHES (Bogenweichen) ===
        
        // BWR - Curved Switch Right R2→R3 (Bogenweiche rechts)
        // Outer rail: R3 (483.75mm), Inner rail: R2 (421.88mm), 30°
        _templates["BWR"] = new TrackGeometry
        {
            ArticleCode = "BWR",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry (R2 inner)
                new TrackPoint(210.94, 56.52),  // [1] R2 exit (30°)
                new TrackPoint(241.88, 64.80)   // [2] R3 exit (30°)
            ],
            EndpointHeadingsDeg = [0, 30, 30],
            PathData = "M 0,0 A 421.88,421.88 0 0 1 210.94,56.52 M 0,0 A 483.75,483.75 0 0 1 241.88,64.80"
        };

        // BWL - Curved Switch Left R2→R3 (Bogenweiche links)
        _templates["BWL"] = new TrackGeometry
        {
            ArticleCode = "BWL",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry
                new TrackPoint(210.94, 56.52),  // [1] R2 exit
                new TrackPoint(241.88, 64.80)   // [2] R3 exit
            ],
            EndpointHeadingsDeg = [0, 30, 30],
            PathData = "M 0,0 A 421.88,421.88 0 0 1 210.94,56.52 M 0,0 A 483.75,483.75 0 0 1 241.88,64.80"
        };

        // BWR-R3 - Curved Switch Right R3→R4 (Bogenweiche rechts R3 zu R4)
        _templates["BWR-R3"] = new TrackGeometry
        {
            ArticleCode = "BWR-R3",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry
                new TrackPoint(241.88, 64.80),  // [1] R3 exit
                new TrackPoint(272.82, 73.08)   // [2] R4 exit
            ],
            EndpointHeadingsDeg = [0, 30, 30],
            PathData = "M 0,0 A 483.75,483.75 0 0 1 241.88,64.80 M 0,0 A 545.63,545.63 0 0 1 272.82,73.08"
        };

        // BWL-R3 - Curved Switch Left R3→R4 (Bogenweiche links R3 zu R4)
        _templates["BWL-R3"] = new TrackGeometry
        {
            ArticleCode = "BWL-R3",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry
                new TrackPoint(241.88, 64.80),  // [1] R3 exit
                new TrackPoint(272.82, 73.08)   // [2] R4 exit
            ],
            EndpointHeadingsDeg = [0, 30, 30],
            PathData = "M 0,0 A 483.75,483.75 0 0 1 241.88,64.80 M 0,0 A 545.63,545.63 0 0 1 272.82,73.08"
        };

        // === CROSSINGS (Kreuzungen) ===
        
        // K15 - 15° Crossing
        // Length based on G231 (230.93mm), crossing angle 15°
        _templates["K15"] = new TrackGeometry
        {
            ArticleCode = "K15",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry A
                new TrackPoint(230.93, 0),      // [1] Exit A
                new TrackPoint(59.70, 15.47),   // [2] Entry B (15° offset)
                new TrackPoint(290.63, 15.47)   // [3] Exit B
            ],
            EndpointHeadingsDeg = [0, 180, 15, 195],
            PathData = "M 0,0 L 230.93,0 M 59.70,15.47 L 290.63,15.47"
        };

        // K30 - 30° Crossing
        // Length based on G107 parallel track (107.32mm)
        _templates["K30"] = new TrackGeometry
        {
            ArticleCode = "K30",
            Endpoints = [
                new TrackPoint(0, 0),           // [0] Entry A
                new TrackPoint(107.32, 0),      // [1] Exit A
                new TrackPoint(27.60, 15.47),   // [2] Entry B (30° offset)
                new TrackPoint(134.92, 15.47)   // [3] Exit B
            ],
            EndpointHeadingsDeg = [0, 180, 30, 210],
            PathData = "M 0,0 L 107.32,0 M 27.60,15.47 L 134.92,15.47"
        };

        // === ANYRAIL IMPORT COMPATIBILITY ===
        // Legacy geometries for AnyRail imports (different radii)

        // Curve_545 - Radius 545mm, 30° (AnyRail XML)
        _templates["Curve_545"] = new TrackGeometry
        {
            ArticleCode = "Curve_545",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(272.5, 73.05)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 545,545 0 0 1 272.5,73.05"
        };

        // Curve_638 - Radius 638mm, 30° (AnyRail XML)
        _templates["Curve_638"] = new TrackGeometry
        {
            ArticleCode = "Curve_638",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(319.0, 85.48)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 638,638 0 0 1 319.0,85.48"
        };

        // Curve_732 - Radius 732mm, 30° (AnyRail XML)
        _templates["Curve_732"] = new TrackGeometry
        {
            ArticleCode = "Curve_732",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(366.0, 98.10)],
            EndpointHeadingsDeg = [0, 30],
            PathData = "M 0,0 A 732,732 0 0 1 366.0,98.10"
        };

        // Curve_1374 - Radius 1374mm, 15° (AnyRail turnout radius)
        _templates["Curve_1374"] = new TrackGeometry
        {
            ArticleCode = "Curve_1374",
            Endpoints = [new TrackPoint(0, 0), new TrackPoint(355.62, 46.80)],
            EndpointHeadingsDeg = [0, 15],
            PathData = "M 0,0 A 1374,1374 0 0 1 355.62,46.80"
        };
    }

    public TrackGeometry? GetGeometry(string articleCode)
    {
        return _templates.TryGetValue(articleCode, out var geom) ? geom : null;
    }

    public void RegisterDynamicGeometry(string articleCode, List<TrackPoint> endpoints, List<double> headingsDeg, string pathData)
    {
        if (_templates.ContainsKey(articleCode))
        {
            return;
        }

        _templates[articleCode] = new TrackGeometry
        {
            ArticleCode = articleCode,
            Endpoints = endpoints,
            EndpointHeadingsDeg = headingsDeg,
            PathData = pathData
        };
    }

    public IEnumerable<string> GetArticleCodes() => _templates.Keys;
}

