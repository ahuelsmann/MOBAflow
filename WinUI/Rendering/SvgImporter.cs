// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Rendering;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>
/// Imports SVG files and renders them onto a WinUI Canvas.
/// Useful for displaying geometry validation results from unit tests.
/// 
/// Supported SVG elements: line, path (arcs), circle, text
/// </summary>
public static partial class SvgImporter
{
    /// <summary>
    /// Loads an SVG file and renders it onto a Canvas.
    /// </summary>
    /// <param name="canvas">Target canvas to render onto.</param>
    /// <param name="svgPath">Path to the SVG file.</param>
    /// <param name="clearCanvas">Whether to clear existing canvas children.</param>
    public static void LoadFromFile(Canvas canvas, string svgPath, bool clearCanvas = true)
    {
        if (!File.Exists(svgPath))
            throw new FileNotFoundException($"SVG file not found: {svgPath}");

        var svgContent = File.ReadAllText(svgPath);
        LoadFromString(canvas, svgContent, clearCanvas);
    }

    /// <summary>
    /// Parses SVG content and renders it onto a Canvas.
    /// </summary>
    /// <param name="canvas">Target canvas to render onto.</param>
    /// <param name="svgContent">SVG content as string.</param>
    /// <param name="clearCanvas">Whether to clear existing canvas children.</param>
    public static void LoadFromString(Canvas canvas, string svgContent, bool clearCanvas = true)
    {
        if (clearCanvas)
            canvas.Children.Clear();

        var doc = XDocument.Parse(svgContent);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        // Process all elements recursively
        ProcessElement(canvas, doc.Root!, ns);
    }

    private static void ProcessElement(Canvas canvas, XElement element, XNamespace ns)
    {
        // Handle transform groups
        if (element.Name.LocalName == "g")
        {
            // For now, we flatten transforms - a full implementation would handle transforms properly
            foreach (var child in element.Elements())
            {
                ProcessElement(canvas, child, ns);
            }
            return;
        }

        // Convert SVG elements to WinUI shapes
        switch (element.Name.LocalName)
        {
            case "line":
                ProcessLine(canvas, element);
                break;
            case "path":
                ProcessPath(canvas, element);
                break;
            case "circle":
                ProcessCircle(canvas, element);
                break;
            case "rect":
                ProcessRect(canvas, element);
                break;
        }

        // Recurse into child elements
        foreach (var child in element.Elements())
        {
            ProcessElement(canvas, child, ns);
        }
    }

    private static void ProcessLine(Canvas canvas, XElement element)
    {
        var line = new Line
        {
            X1 = ParseDouble(element.Attribute("x1")?.Value),
            Y1 = ParseDouble(element.Attribute("y1")?.Value),
            X2 = ParseDouble(element.Attribute("x2")?.Value),
            Y2 = ParseDouble(element.Attribute("y2")?.Value),
            Stroke = ParseBrush(element.Attribute("stroke")?.Value ?? GetStyleAttribute(element, "stroke") ?? "#333333"),
            StrokeThickness = ParseDouble(element.Attribute("stroke-width")?.Value ?? GetStyleAttribute(element, "stroke-width") ?? "1")
        };

        canvas.Children.Add(line);
    }

    private static void ProcessPath(Canvas canvas, XElement element)
    {
        var pathData = element.Attribute("d")?.Value;
        if (string.IsNullOrEmpty(pathData))
            return;

        try
        {
            var geometry = (Geometry)XamlReader.Load(
                $"<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{ConvertSvgPathToXaml(pathData)}</Geometry>");

            var path = new Path
            {
                Data = geometry,
                Stroke = ParseBrush(element.Attribute("stroke")?.Value ?? GetStyleAttribute(element, "stroke") ?? "#333333"),
                StrokeThickness = ParseDouble(element.Attribute("stroke-width")?.Value ?? GetStyleAttribute(element, "stroke-width") ?? "1"),
                Fill = null
            };

            canvas.Children.Add(path);
        }
        catch
        {
            // Skip paths that can't be parsed
        }
    }

    private static void ProcessCircle(Canvas canvas, XElement element)
    {
        var cx = ParseDouble(element.Attribute("cx")?.Value);
        var cy = ParseDouble(element.Attribute("cy")?.Value);
        var r = ParseDouble(element.Attribute("r")?.Value);

        var ellipse = new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Fill = ParseBrush(element.Attribute("fill")?.Value ?? GetStyleAttribute(element, "fill") ?? "transparent"),
            Stroke = ParseBrush(element.Attribute("stroke")?.Value ?? GetStyleAttribute(element, "stroke") ?? "#333333"),
            StrokeThickness = ParseDouble(element.Attribute("stroke-width")?.Value ?? GetStyleAttribute(element, "stroke-width") ?? "1")
        };

        Canvas.SetLeft(ellipse, cx - r);
        Canvas.SetTop(ellipse, cy - r);
        canvas.Children.Add(ellipse);
    }

    private static void ProcessRect(Canvas canvas, XElement element)
    {
        var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
        {
            Width = ParseDouble(element.Attribute("width")?.Value),
            Height = ParseDouble(element.Attribute("height")?.Value),
            Fill = ParseBrush(element.Attribute("fill")?.Value ?? GetStyleAttribute(element, "fill") ?? "transparent"),
            Stroke = ParseBrush(element.Attribute("stroke")?.Value ?? GetStyleAttribute(element, "stroke") ?? "#333333"),
            StrokeThickness = ParseDouble(element.Attribute("stroke-width")?.Value ?? GetStyleAttribute(element, "stroke-width") ?? "1")
        };

        Canvas.SetLeft(rect, ParseDouble(element.Attribute("x")?.Value));
        Canvas.SetTop(rect, ParseDouble(element.Attribute("y")?.Value));
        canvas.Children.Add(rect);
    }

    private static string? GetStyleAttribute(XElement element, string attributeName)
    {
        var style = element.Attribute("style")?.Value;
        if (string.IsNullOrEmpty(style))
            return null;

        var match = Regex.Match(style, $@"{attributeName}\s*:\s*([^;]+)");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string ConvertSvgPathToXaml(string svgPath)
    {
        // SVG and XAML path syntax are mostly compatible
        // Main difference: SVG uses lowercase for relative, XAML uses same commands
        return svgPath;
    }

    private static double ParseDouble(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        // Remove units like "px"
        value = Regex.Replace(value, @"[a-zA-Z]+$", "");
        return double.TryParse(value, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static SolidColorBrush ParseBrush(string color)
    {
        if (string.IsNullOrEmpty(color) || color == "none" || color == "transparent")
            return new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        try
        {
            if (color.StartsWith('#'))
            {
                color = color.TrimStart('#');
                if (color.Length == 6)
                {
                    var r = Convert.ToByte(color[..2], 16);
                    var g = Convert.ToByte(color[2..4], 16);
                    var b = Convert.ToByte(color[4..6], 16);
                    return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
                }
            }

            // Try named colors
            return color.ToLowerInvariant() switch
            {
                "red" => new SolidColorBrush(Microsoft.UI.Colors.Red),
                "green" => new SolidColorBrush(Microsoft.UI.Colors.Green),
                "blue" => new SolidColorBrush(Microsoft.UI.Colors.Blue),
                "orange" => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                "gray" or "grey" => new SolidColorBrush(Microsoft.UI.Colors.Gray),
                "black" => new SolidColorBrush(Microsoft.UI.Colors.Black),
                "white" => new SolidColorBrush(Microsoft.UI.Colors.White),
                _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
        }
        catch
        {
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }
    }
}
