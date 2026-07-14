// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Renderer;

/// <summary>
/// Tracks min/max coordinates while rendering SVG segments.
/// </summary>
internal sealed class TrackPlanSvgBoundsTracker
{
    public double MinX { get; private set; } = double.MaxValue;
    public double MinY { get; private set; } = double.MaxValue;
    public double MaxX { get; private set; } = double.MinValue;
    public double MaxY { get; private set; } = double.MinValue;

    public void Reset()
    {
        MinX = double.MaxValue;
        MinY = double.MaxValue;
        MaxX = double.MinValue;
        MaxY = double.MinValue;
    }

    public void Include(double x, double y)
    {
        MinX = Math.Min(MinX, x);
        MinY = Math.Min(MinY, y);
        MaxX = Math.Max(MaxX, x);
        MaxY = Math.Max(MaxY, y);
    }
}

/// <summary>
/// Alternating port color scheme for track-plan SVG rendering.
/// </summary>
internal static class TrackPlanSvgPortColorScheme
{
    public static string GetPortColor(char port, int segmentIndex)
    {
        var scheme = segmentIndex % 2;

        return scheme switch
        {
            0 => port switch
            {
                'A' => "#000000",
                'B' => "#FF0000",
                'C' => "#00FF00",
                'D' => "#0000FF",
                _ => "#888888"
            },
            _ => port switch
            {
                'A' => "#808080",
                'B' => "#FF00FF",
                'C' => "#FFD700",
                'D' => "#00FFFF",
                _ => "#888888"
            }
        };
    }
}