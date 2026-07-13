// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackPlan.Renderer;

using System.Globalization;
using System.Text;

using TrackLibrary.PikoA;

public sealed class PlacedTrackPlanSvgRenderer
{
    private const double Margin = 50.0;
    private const double GridSpacingMm = 100.0;

    public string Render(IReadOnlyList<PlacedSegment> placements, double trackOpacity = 0.8, bool showGrid = false, bool showPorts = false)
    {
        ArgumentNullException.ThrowIfNull(placements);

        var scene = TrackPlanRenderSceneBuilder.Build(placements);
        return Render(scene, trackOpacity, showGrid, showPorts ? placements : null);
    }

    /// <summary>Renders only the renderer-neutral scene used by Win2D and future adapters.</summary>
    public string Render(TrackPlanRenderScene scene, double trackOpacity = 0.8, bool showGrid = false)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return Render(scene, trackOpacity, showGrid, null);
    }

    private string Render(TrackPlanRenderScene scene, double trackOpacity, bool showGrid, IReadOnlyList<PlacedSegment>? placementsForPorts)
    {
        if (scene.Items.Count == 0)
            return "<svg></svg>";

        var bounds = ComputeBounds(scene.Items, scene.Markers);
        var minX = bounds.MinX - Margin;
        var minY = bounds.MinY - Margin;
        var maxX = bounds.MaxX + Margin;
        var maxY = bounds.MaxY + Margin;
        var width = maxX - minX;
        var height = maxY - minY;

        var builder = new StringBuilder();
        builder.AppendLine($"<svg width=\"{F(width)}\" height=\"{F(height)}\" viewBox=\"{F(minX)} {F(minY)} {F(width)} {F(height)}\" xmlns=\"http://www.w3.org/2000/svg\">");

        if (showGrid)
            AppendGrid(builder, minX, minY, maxX, maxY);

        foreach (var item in scene.Items)
        {
            var svgPath = PathToSvgConverter.ToSvgPath(item.Path, item.X, item.Y, item.RotationDegrees);
            var stroke = item.IsSelected ? "#0078D4" : "#333333";
            if (item.FeedbackIntensity > 0)
                builder.AppendLine($"  <path d=\"{svgPath}\" stroke=\"#FFB400\" stroke-width=\"14\" stroke-opacity=\"{F(item.FeedbackIntensity)}\" fill=\"none\" />");
            builder.AppendLine($"  <path d=\"{svgPath}\" stroke=\"{stroke}\" stroke-width=\"{(item.IsSelected ? 10 : 4)}\" stroke-opacity=\"{trackOpacity.ToString("F2", CultureInfo.InvariantCulture)}\" fill=\"none\" />");

            if (!string.IsNullOrWhiteSpace(item.Label))
                builder.AppendLine($"  <text x=\"{F(item.X)}\" y=\"{F(item.Y - 12)}\" font-size=\"12\" fill=\"#333333\">{Escape(item.Label)}</text>");
        }

        if (placementsForPorts != null)
        {
            foreach (var placed in placementsForPorts)
            {
            foreach (var (portName, x, y, _) in SegmentPortGeometry.GetAllPortWorldPositions(placed))
            {
                var color = GetPortColor(portName);
                builder.AppendLine($"  <circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"6\" fill=\"{color}\" fill-opacity=\"0.9\" />");
                builder.AppendLine($"  <text x=\"{F(x + 10)}\" y=\"{F(y - 10)}\" font-size=\"12\" font-weight=\"bold\" fill=\"{color}\">{portName[^1]}</text>");
            }
            }
        }

        foreach (var marker in scene.Markers)
        {
            builder.AppendLine($"  <circle cx=\"{F(marker.X)}\" cy=\"{F(marker.Y)}\" r=\"8\" fill=\"#E81123\" fill-opacity=\"0.7\" />");
            builder.AppendLine($"  <text x=\"{F(marker.X + 10)}\" y=\"{F(marker.Y - 10)}\" font-size=\"12\" fill=\"#A80000\">{Escape(marker.Message)}</text>");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static void AppendGrid(StringBuilder builder, double minX, double minY, double maxX, double maxY)
    {
        var startX = Math.Floor(minX / GridSpacingMm) * GridSpacingMm;
        var startY = Math.Floor(minY / GridSpacingMm) * GridSpacingMm;

        for (var x = startX; x <= maxX; x += GridSpacingMm)
        {
            var stroke = Math.Abs(x % (GridSpacingMm * 5)) < 0.001 ? "#D0D0D0" : "#ECECEC";
            var width = Math.Abs(x % (GridSpacingMm * 5)) < 0.001 ? "1.5" : "1";
            builder.AppendLine($"  <line x1=\"{F(x)}\" y1=\"{F(minY)}\" x2=\"{F(x)}\" y2=\"{F(maxY)}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />");
        }

        for (var y = startY; y <= maxY; y += GridSpacingMm)
        {
            var stroke = Math.Abs(y % (GridSpacingMm * 5)) < 0.001 ? "#D0D0D0" : "#ECECEC";
            var width = Math.Abs(y % (GridSpacingMm * 5)) < 0.001 ? "1.5" : "1";
            builder.AppendLine($"  <line x1=\"{F(minX)}\" y1=\"{F(y)}\" x2=\"{F(maxX)}\" y2=\"{F(y)}\" stroke=\"{stroke}\" stroke-width=\"{width}\" />");
        }
    }

    private static (double MinX, double MinY, double MaxX, double MaxY) ComputeBounds(
        IReadOnlyList<TrackPlanRenderItem> items,
        IReadOnlyList<TrackPlanValidationMarker> markers)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var item in items)
        {
            var (localMinX, localMinY, localMaxX, localMaxY) = SegmentLocalPathBuilder.GetBounds(item.Path);

            var angleRad = item.RotationDegrees * Math.PI / 180;
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            static double Tx(double ox, double lx, double ly, double cos, double sin) => ox + lx * cos - ly * sin;
            static double Ty(double oy, double lx, double ly, double cos, double sin) => oy + lx * sin + ly * cos;

            var corners = new[]
            {
                (Tx(item.X, localMinX, localMinY, cos, sin), Ty(item.Y, localMinX, localMinY, cos, sin)),
                (Tx(item.X, localMaxX, localMinY, cos, sin), Ty(item.Y, localMaxX, localMinY, cos, sin)),
                (Tx(item.X, localMinX, localMaxY, cos, sin), Ty(item.Y, localMinX, localMaxY, cos, sin)),
                (Tx(item.X, localMaxX, localMaxY, cos, sin), Ty(item.Y, localMaxX, localMaxY, cos, sin))
            };

            foreach (var (x, y) in corners)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        foreach (var marker in markers)
        {
            minX = Math.Min(minX, marker.X);
            minY = Math.Min(minY, marker.Y);
            maxX = Math.Max(maxX, marker.X);
            maxY = Math.Max(maxY, marker.Y);
        }

        return (minX, minY, maxX, maxY);
    }

    private static string GetPortColor(string portName)
    {
        return portName switch
        {
            "PortA" => "#000000",
            "PortB" => "#FF0000",
            "PortC" => "#00AA00",
            "PortD" => "#0000FF",
            _ => "#666666"
        };
    }

    private static string F(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;
}
