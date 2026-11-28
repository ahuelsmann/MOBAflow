// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.TrackPlanAsset;

using System.Text.Json;
using System.Xml.Linq;

using Moba.Domain;
using Moba.Test.Helpers;
using Moba.TrackPlan.Editor;
using Moba.TrackPlan.Renderer;

internal static class DemoTrackPlanAsset
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Render(string samplePath)
    {
        using var input = File.OpenRead(samplePath);
        var sample = JsonSerializer.Deserialize<TrackPlanDocument>(input, JsonOptions)
                     ?? throw new InvalidDataException("The shipped track plan could not be read.");
        var document = TrackPlanEditorDocument.FromDomainDocument(sample);
        var (placements, _) = document.ToEditableTrackPlanData(TrackPlanTestGeometry.Catalog);
        var scene = TrackPlanRenderSceneBuilder.Build(TrackPlanTestGeometry.PikoA, placements);
        return ApplyDemoPalette(new PlacedTrackPlanSvgRenderer().Render(scene));
    }

    private static string ApplyDemoPalette(string svg)
    {
        var root = XElement.Parse(svg, LoadOptions.PreserveWhitespace);
        var ns = root.Name.Namespace;
        var viewBox = root.Attribute("viewBox")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      ?? throw new InvalidDataException("The rendered track plan has no viewBox.");

        root.AddFirst("\n  ", new XElement(ns + "rect",
            new XAttribute("class", "track-plan-surface"),
            new XAttribute("x", viewBox[0]),
            new XAttribute("y", viewBox[1]),
            new XAttribute("width", viewBox[2]),
            new XAttribute("height", viewBox[3]),
            new XAttribute("fill", "#0a0a0a")));

        foreach (var path in root.Elements(ns + "path"))
        {
            path.SetAttributeValue("class", "track-plan-rail");
            path.SetAttributeValue("stroke", "#dedede");
            path.SetAttributeValue("stroke-opacity", null);
        }

        foreach (var label in root.Elements(ns + "text"))
        {
            label.SetAttributeValue("class", "track-plan-label");
            label.SetAttributeValue("fill", "#f2f2f2");
        }

        return root.ToString(SaveOptions.DisableFormatting).ReplaceLineEndings("\n") + "\n";
    }
}
