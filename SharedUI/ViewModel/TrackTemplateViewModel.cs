// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Domain.TrackPlan;

/// <summary>
/// ViewModel wrapper for TrackTemplate (from PikoATrackLibrary).
/// Used in the toolbar list for drag & drop track planning.
/// </summary>
public class TrackTemplateViewModel
{
    #region Fields
    private readonly TrackTemplate _template;
    #endregion

    public TrackTemplateViewModel(TrackTemplate template)
    {
        _template = template;
    }

    #region Domain Properties (1:1 mapping)
    public string ArticleCode => _template.ArticleCode;
    public string Name => _template.Name;
    public TrackType Type => _template.Type;
    public double Length => _template.Length;
    public double Radius => _template.Radius;
    public double Angle => _template.Angle;
    public string ProductCode => _template.ProductCode;
    public string DisplayText => _template.DisplayText;
    public string Description => _template.Description;
    #endregion

    #region UI Properties
    /// <summary>
    /// Icon glyph for track type (Segoe MDL2 Assets).
    /// </summary>
    public string IconGlyph => Type switch
    {
        TrackType.Straight => "\uE8DC",        // Line
        TrackType.Curve => "\uE913",           // Arc
        TrackType.TurnoutLeft => "\uE76B",     // ArrowLeft
        TrackType.TurnoutRight => "\uE76C",    // ArrowRight
        TrackType.CurvedTurnoutLeft => "\uE919",   // Flow
        TrackType.CurvedTurnoutRight => "\uE91A",  // Flow
        TrackType.DoubleCrossover => "\uE8AB", // MultiSelect
        TrackType.ThreeWay => "\uE8FD",        // Split
        TrackType.Crossing => "\uE8DB",        // IntersectShape
        _ => "\uE8DC"
    };

    /// <summary>
    /// Category for grouping in toolbar.
    /// </summary>
    public string Category => Type switch
    {
        TrackType.Straight => "Gerade Gleise",
        TrackType.Curve => "Bogengleise",
        TrackType.TurnoutLeft or TrackType.TurnoutRight 
            or TrackType.CurvedTurnoutLeft or TrackType.CurvedTurnoutRight
            or TrackType.DoubleCrossover or TrackType.ThreeWay => "Weichen",
        TrackType.Crossing => "Kreuzungen",
        _ => "Sonstiges"
    };
    #endregion
}
