// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.PikoA.Catalog;

using Moba.TrackLibrary.PikoA.Template;
using Moba.TrackPlan.TrackSystem;

/// <summary>
/// Complete catalog of Piko A-Gleis track pieces.
/// Implements ITrackCatalog for use in the track plan editor.
/// </summary>
public sealed class PikoATrackCatalog : ITrackCatalog
{
    public string SystemId => "PikoA";
    public string SystemName => "Piko A-Gleis";
    public string Manufacturer => "Piko";
    public string Scale => "H0";

    /// <summary>
    /// All templates (Straights, Curves, Switches) combined.
    /// </summary>
    public IReadOnlyList<TrackTemplate> Templates { get; } = BuildTemplateList();

    /// <summary>
    /// Straight track templates (G231, G119, …)
    /// </summary>
    public IEnumerable<TrackTemplate> Straights => StraightTemplates.All;

    /// <summary>
    /// Curve track templates (R1, R2, R3, R9)
    /// </summary>
    public IEnumerable<TrackTemplate> Curves => CurveTemplates.All;

    /// <summary>
    /// Switch templates (BWL, BWR, K30)
    /// </summary>
    public IEnumerable<TrackTemplate> Switches => SwitchTemplates.All;

    /// <summary>
    /// Lookup a template by its ID (e.g. "G231", "R1", "BWL").
    /// </summary>
    public TrackTemplate? GetById(string id)
        => Templates.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Lookup templates by geometry category (Straight, Curve, Switch).
    /// </summary>
    public IEnumerable<TrackTemplate> GetByCategory(TrackGeometryKind kind)
        => Templates.Where(t => t.Geometry.GeometryKind == kind);

    /// <summary>
    /// Build the combined template list.
    /// </summary>
    private static List<TrackTemplate> BuildTemplateList()
    {
        var list = new List<TrackTemplate>();
        list.AddRange(StraightTemplates.All);
        list.AddRange(CurveTemplates.All);
        list.AddRange(SwitchTemplates.All);
        return list;
    }
}
