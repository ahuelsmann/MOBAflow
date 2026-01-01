using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Moba.TrackPlan.Renderer;

/// <summary>
/// Transforms SVG PathData by applying 2D affine transformations (rotation + translation).
/// Parses "M x,y L x,y A rx,ry rotation large-arc sweep x,y" commands and applies world transforms.
/// </summary>
public static class PathDataTransformer
{
    public static string Transform(string localPathData, double worldX, double worldY, double rotationDegrees)
    {
        if (string.IsNullOrWhiteSpace(localPathData))
            return localPathData;

        var radians = rotationDegrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        var result = new StringBuilder();
        var commands = SplitPathCommands(localPathData);

        foreach (var cmd in commands)
        {
            var trimmed = cmd.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var commandType = trimmed[0];
            var parameters = trimmed.Substring(1).Trim();

            switch (commandType)
            {
                case 'M':
                case 'L':
                    {
                        var point = ParsePoint(parameters);
                        var transformed = ApplyTransform(point.X, point.Y, worldX, worldY, cos, sin);
                        result.Append($"{commandType} {FormatNumber(transformed.X)},{FormatNumber(transformed.Y)} ");
                        break;
                    }

                case 'A':
                    {
                        var parts = parameters.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 7)
                        {
                            var rx = double.Parse(parts[0], CultureInfo.InvariantCulture);
                            var ry = double.Parse(parts[1], CultureInfo.InvariantCulture);
                            var rotation = double.Parse(parts[2], CultureInfo.InvariantCulture);
                            var largeArc = parts[3];
                            var sweep = parts[4];
                            var endX = double.Parse(parts[5], CultureInfo.InvariantCulture);
                            var endY = double.Parse(parts[6], CultureInfo.InvariantCulture);

                            var transformedEnd = ApplyTransform(endX, endY, worldX, worldY, cos, sin);
                            var newRotation = rotation + rotationDegrees;

                            result.Append($"A {FormatNumber(rx)},{FormatNumber(ry)} {FormatNumber(newRotation)} {largeArc} {sweep} {FormatNumber(transformedEnd.X)},{FormatNumber(transformedEnd.Y)} ");
                        }
                        break;
                    }

                default:
                    result.Append(trimmed + " ");
                    break;
            }
        }

        return result.ToString().Trim();
    }

    private static string[] SplitPathCommands(string pathData)
    {
        return Regex.Split(pathData, @"(?=[MLAZC])")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
    }

    private static (double X, double Y) ParsePoint(string pointStr)
    {
        var coords = pointStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var x = double.Parse(coords[0], CultureInfo.InvariantCulture);
        var y = double.Parse(coords[1], CultureInfo.InvariantCulture);
        return (x, y);
    }

    private static (double X, double Y) ApplyTransform(
        double localX, double localY,
        double worldX, double worldY,
        double cos, double sin)
    {
        var rotatedX = localX * cos - localY * sin;
        var rotatedY = localX * sin + localY * cos;

        return (rotatedX + worldX, rotatedY + worldY);
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
