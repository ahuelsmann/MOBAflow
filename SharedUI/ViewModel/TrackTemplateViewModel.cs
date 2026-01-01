// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.TrackPlan.Domain;

/// <summary>
/// ViewModel wrapper for TrackTemplate (from PikoATrackLibrary).
/// Used in the toolbar list for drag & drop track planning.
/// </summary>
public class TrackTemplateViewModel
{
    private readonly TrackTemplate _template;

    public TrackTemplateViewModel(TrackTemplate template)
    {
        _template = template;
    }

    public string ArticleCode => _template.ArticleCode;
    public string Name => _template.Name;
    public TrackType Type => _template.Type;
    public double Length => _template.Length;
    public double Radius => _template.Radius;
    public double Angle => _template.Angle;
    public string ProductCode => _template.ProductCode;
    public string DisplayText => _template.DisplayText;
    public string Description => _template.Description;

    public string IconGlyph => Type switch
    {
        TrackType.Straight => "\uE8DC",
        TrackType.Curve => "\uE913",
        TrackType.TurnoutLeft => "\uE76B",
        TrackType.TurnoutRight => "\uE76C",
        TrackType.CurvedTurnoutLeft => "\uE919",
        TrackType.CurvedTurnoutRight => "\uE91A",
        TrackType.DoubleCrossover => "\uE8AB",
        TrackType.ThreeWay => "\uE8FD",
        TrackType.Crossing => "\uE8DB",
        _ => "\uE8DC"
    };

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
}
