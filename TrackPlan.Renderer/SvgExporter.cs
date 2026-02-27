namespace Moba.TrackPlan.Renderer;

/// <summary>
/// Exports SVG-based track plans as HTML files that can be viewed in a browser.
/// </summary>
public class SvgExporter
{
    /// <summary>
    /// Writes the given SVG content wrapped in a minimal HTML page to the specified file path.
    /// </summary>
    /// <param name="svgContent">The raw SVG markup representing the track plan.</param>
    /// <param name="filePath">The target file path for the generated HTML file.</param>
    public void Export(string svgContent, string filePath)
    {
        // HTML wrapper for better browser display
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Track Plan</title>
    <style>
        body {{
            margin: 20px;
            background-color: #f5f5f5;
            font-family: Arial, sans-serif;
        }}
        .container {{
            background: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            display: inline-block;
        }}
        h1 {{
            margin-top: 0;
            color: #333;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Track Plan Visualization</h1>
        {svgContent}
    </div>
</body>
</html>";

        File.WriteAllText(filePath, html);
    }
}