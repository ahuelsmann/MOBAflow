namespace Moba.TrackPlan.Renderer;

public class SvgExporter
{
    public void Export(string svgContent, string filePath)
    {
        // HTML-Wrapper für bessere Browser-Darstellung
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