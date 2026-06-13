using System.Text.RegularExpressions;

using SkiaSharp;
using Svg.Skia;

var repoRoot = FindRepositoryRoot();
var sourceDirectory = Path.Combine(repoRoot, "MOBAflow", "Assets");
var outputDirectory = Path.Combine(sourceDirectory, "FunctionSymbols");
var sizes = new[] { 20, 32 };
var themes = new (string Name, string Color)[]
{
    ("light", "#000000"),
    ("dark", "#FFFFFF")
};

var excludedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "DoorClose.svg",
    "DoorOpen.svg",
    "IsDoorBlocked.svg",
    "mobaflow-icon.svg"
};

var svgFiles = Directory.EnumerateFiles(sourceDirectory, "*.svg", SearchOption.TopDirectoryOnly)
    .Where(path => !excludedAssets.Contains(Path.GetFileName(path)))
    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
    .ToList();

foreach (var theme in themes)
{
    foreach (var size in sizes)
    {
        var targetDirectory = Path.Combine(outputDirectory, theme.Name, size.ToString());
        Directory.CreateDirectory(targetDirectory);

        foreach (var sourcePath in svgFiles)
        {
            var outputPath = Path.Combine(targetDirectory, Path.ChangeExtension(Path.GetFileName(sourcePath), ".png"));
            RenderSymbol(sourcePath, outputPath, theme.Color, size);
        }
    }
}

Console.WriteLine($"Rendered {svgFiles.Count} function symbols in {themes.Length * sizes.Length} variants.");

static string FindRepositoryRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current != null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, "MOBAflow", "Assets")))
            return current.FullName;

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find repository root containing MOBAflow/Assets.");
}

static void RenderSymbol(string sourcePath, string outputPath, string color, int size)
{
    var svgText = File.ReadAllText(sourcePath);
    var coloredSvg = Recolor(svgText, color);

    using var svg = SKSvg.CreateFromSvg(coloredSvg);
    var picture = svg.Picture ?? throw new InvalidOperationException($"Could not load SVG: {sourcePath}");
    var bounds = picture.CullRect;
    if (bounds.Width <= 0 || bounds.Height <= 0)
        throw new InvalidOperationException($"SVG has invalid bounds: {sourcePath}");

    using var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColors.Transparent);

    var scale = Math.Min(size / bounds.Width, size / bounds.Height);
    var offsetX = (size - bounds.Width * scale) / 2f;
    var offsetY = (size - bounds.Height * scale) / 2f;

    canvas.Translate(offsetX, offsetY);
    canvas.Scale(scale);
    canvas.Translate(-bounds.Left, -bounds.Top);
    canvas.DrawPicture(picture);
    canvas.Flush();

    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var stream = File.Create(outputPath);
    data.SaveTo(stream);
}

static string Recolor(string svgText, string color)
{
    const string colorValuePattern = @"currentColor|black|#000|#000000|rgb\(0,\s*0,\s*0\)";
    var options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    var modified = Regex.Replace(
        svgText,
        $@"(?<name>stroke|fill)=[""'](?<value>{colorValuePattern})[""']",
        m => $"{m.Groups["name"].Value}=\"{color}\"",
        options);

    modified = Regex.Replace(
        modified,
        $@"(?<name>stroke|fill)=(?<value>{colorValuePattern})\b",
        m => $"{m.Groups["name"].Value}=\"{color}\"",
        options);

    return Regex.Replace(
        modified,
        $@"(?<name>stroke|fill)\s*:\s*(?<value>{colorValuePattern})\b",
        m => $"{m.Groups["name"].Value}:{color}",
        options);
}
