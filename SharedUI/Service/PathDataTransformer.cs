using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Moba.SharedUI.Service;

/// <summary>
/// Transforms SVG PathData by applying 2D affine transformations (rotation + translation).
/// Parses "M x,y L x,y A rx,ry rotation large-arc sweep x,y" commands and applies world transforms.
/// </summary>
public static class PathDataTransformer
{
    /// <summary>
    /// Transforms PathData from local coordinates to world coordinates.
    /// </summary>
    /// <param name="localPathData">Original PathData in local coordinates (e.g., "M 0,0 L 350,0")</param>
    /// <param name="worldX">World X translation</param>
    /// <param name="worldY">World Y translation</param>
    /// <param name="rotationDegrees">Rotation in degrees (clockwise)</param>
    /// <returns>Transformed PathData in world coordinates</returns>
    public static string Transform(string localPathData, double worldX, double worldY, double rotationDegrees)
    {
        if (string.IsNullOrWhiteSpace(localPathData))
            return localPathData;

        // Pre-compute rotation matrix (clockwise rotation in screen coordinates)
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
                case 'M': // Move (absolute)
                case 'L': // Line (absolute)
                    {
                        var point = ParsePoint(parameters);
                        var transformed = ApplyTransform(point.X, point.Y, worldX, worldY, cos, sin);
                        result.Append($"{commandType} {FormatNumber(transformed.X)},{FormatNumber(transformed.Y)} ");
                        break;
                    }

                case 'A': // Arc (absolute)
                    {
                        // Format: A rx,ry rotation large-arc sweep x,y
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

                            // Transform endpoint
                            var transformedEnd = ApplyTransform(endX, endY, worldX, worldY, cos, sin);

                            // Adjust arc rotation by adding world rotation
                            var newRotation = rotation + rotationDegrees;

                            result.Append($"A {FormatNumber(rx)},{FormatNumber(ry)} {FormatNumber(newRotation)} {largeArc} {sweep} {FormatNumber(transformedEnd.X)},{FormatNumber(transformedEnd.Y)} ");
                        }
                        break;
                    }

                default:
                    // Unsupported command - pass through unchanged (shouldn't happen with TrackGeometryLibrary)
                    result.Append(trimmed + " ");
                    break;
            }
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// Splits PathData string into individual commands (handles "M 0,0 L 350,0 A ..." format).
    /// </summary>
    private static string[] SplitPathCommands(string pathData)
    {
        // Split on command letters (M, L, A, etc.)
        return Regex.Split(pathData, @"(?=[MLAZC])")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
    }

    /// <summary>
    /// Parses "x,y" or "x y" format into a point.
    /// </summary>
    private static (double X, double Y) ParsePoint(string pointStr)
    {
        var coords = pointStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var x = double.Parse(coords[0], CultureInfo.InvariantCulture);
        var y = double.Parse(coords[1], CultureInfo.InvariantCulture);
        return (x, y);
    }

    /// <summary>
    /// Applies 2D affine transformation: rotate around origin, then translate.
    /// </summary>
    private static (double X, double Y) ApplyTransform(
        double localX, double localY,
        double worldX, double worldY,
        double cos, double sin)
    {
        // Rotate around origin (0, 0)
        var rotatedX = localX * cos - localY * sin;
        var rotatedY = localX * sin + localY * cos;

        // Translate to world position
        return (rotatedX + worldX, rotatedY + worldY);
    }

    /// <summary>
    /// Formats number with 1 decimal place precision (sufficient for rendering).
    /// </summary>
    private static string FormatNumber(double value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
