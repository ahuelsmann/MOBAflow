// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Moba.TrackLibrary.PikoA;
using Moba.TrackPlan.Editor;

/// <summary>
/// Piko A catalogue and placement geometry shared by track-plan editor tests.
/// </summary>
internal static class TrackPlanTestGeometry
{
    public static PikoATrackCatalog Catalog { get; } = new();

    public static SegmentPlacementGeometry PikoA { get; } = new(Catalog);
}
