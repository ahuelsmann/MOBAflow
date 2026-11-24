// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.TrackPlanAsset;

using System.Text.Json;

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
        return new PlacedTrackPlanSvgRenderer().Render(scene).ReplaceLineEndings("\n");
    }
}
