namespace Moba.TrackPlan.Renderer;

using System.Globalization;
using System.Text;
using TrackLibrary.PikoA;

/// <summary>
/// Konvertiert Pfad-Befehle aus <see cref="SegmentLocalPathBuilder"/> in SVG-Pfad-Strings.
/// Transformiert lokale Koordinaten (Port A = Ursprung) in Weltkoordinaten.
/// </summary>
public static class PathToSvgConverter
{
    /// <summary>
    /// Konvertiert Pfad-Befehle mit einer Translation und Rotation in einen SVG path-d-String.
    /// </summary>
    /// <param name="commands">Pfad-Befehle in lokalen Koordinaten</param>
    /// <param name="originX">X der Position (Port A) in Weltkoordinaten</param>
    /// <param name="originY">Y der Position (Port A) in Weltkoordinaten</param>
    /// <param name="angleDegrees">Rotation in Grad (0 = +X)</param>
    /// <returns>SVG path "d"-Attribut (ohne das d="-Zeichen)</returns>
    public static string ToSvgPath(
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands,
        double originX,
        double originY,
        double angleDegrees)
    {
        var angleRad = angleDegrees * Math.PI / 180;
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);

        double Tx(double lx, double ly) => originX + lx * cos - ly * sin;
        double Ty(double lx, double ly) => originY + lx * sin + ly * cos;

        var sb = new StringBuilder();
        double x = 0, y = 0;

        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case SegmentLocalPathBuilder.MoveTo move:
                    x = move.X;
                    y = move.Y;
                    break;
                case SegmentLocalPathBuilder.LineTo line:
                    {
                        var x1 = Tx(x, y);
                        var y1 = Ty(x, y);
                        var x2 = Tx(line.X, line.Y);
                        var y2 = Ty(line.X, line.Y);
                        sb.Append($"M {F(x1)},{F(y1)} L {F(x2)},{F(y2)} ");
                        x = line.X;
                        y = line.Y;
                        break;
                    }
                case SegmentLocalPathBuilder.ArcTo arc:
                    {
                        var x1 = Tx(x, y);
                        var y1 = Ty(x, y);
                        var x2 = Tx(arc.EndX, arc.EndY);
                        var y2 = Ty(arc.EndX, arc.EndY);
                        var largeArc = arc.LargeArc ? 1 : 0;
                        var sweep = arc.Clockwise ? 1 : 0;
                        sb.Append($"M {F(x1)},{F(y1)} A {F(arc.Radius)},{F(arc.Radius)} 0 {largeArc},{sweep} {F(x2)},{F(y2)} ");
                        x = arc.EndX;
                        y = arc.EndY;
                        break;
                    }
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Erzeugt einen Pfad-Data-String in lokalen Koordinaten (Skalierung + optionaler Offset).
    /// Format ist kompatibel mit WinUI Path.Data und SVG path "d".
    /// </summary>
    public static string ToPathDataString(
        IReadOnlyList<SegmentLocalPathBuilder.PathCommand> commands,
        double scale = 1.0,
        double offsetX = 0,
        double offsetY = 0)
    {
        double Tx(double x) => (x + offsetX) * scale;
        double Ty(double y) => (y + offsetY) * scale;

        var sb = new StringBuilder();
        double x = 0, y = 0;

        foreach (var cmd in commands)
        {
            switch (cmd)
            {
                case SegmentLocalPathBuilder.MoveTo move:
                    x = move.X;
                    y = move.Y;
                    break;
                case SegmentLocalPathBuilder.LineTo line:
                    sb.Append($"M {F(Tx(x))},{F(Ty(y))} L {F(Tx(line.X))},{F(Ty(line.Y))} ");
                    x = line.X;
                    y = line.Y;
                    break;
                case SegmentLocalPathBuilder.ArcTo arc:
                    {
                        var rx = arc.Radius * scale;
                        var ry = arc.Radius * scale;
                        var largeArc = arc.LargeArc ? 1 : 0;
                        var sweep = arc.Clockwise ? 1 : 0;
                        sb.Append($"M {F(Tx(x))},{F(Ty(y))} A {F(rx)},{F(ry)} 0 {largeArc},{sweep} {F(Tx(arc.EndX))},{F(Ty(arc.EndY))} ");
                        x = arc.EndX;
                        y = arc.EndY;
                        break;
                    }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string F(double v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
