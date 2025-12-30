using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Geometry;

static bool IsProbablyMeasurementToken(string text)
{
    if (string.IsNullOrWhiteSpace(text)) return false;
    // Keep it simple: must contain a digit and optionally comma/dot/degree/quote.
    return text.Any(char.IsDigit) && text.Length <= 12;
}

static IEnumerable<int> ParsePagesArg(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return [];

    return value
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(s => int.TryParse(s, out var v) ? v : -1)
        .Where(v => v > 0)
        .Distinct()
        .OrderBy(v => v)
        .ToArray();
}

var pdfPath = args.Length > 0 ? args[0] : Path.Combine("..", "..", "..", "..", "docs", "Piko A Gleis.pdf");
var pagesFilter = args.Length > 1 ? ParsePagesArg(args[1]) : new[] { 18, 19 };
var reportOnly = args.Length > 2 && args[2].Equals("report", StringComparison.OrdinalIgnoreCase);

if (!File.Exists(pdfPath))
{
    Console.Error.WriteLine($"PDF not found: {pdfPath}");
    return 2;
}

var interestingTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "WL","WR","BWL","BWR","BWL-R3","BWR-R3","K15","K30","DKW","W3","WY",
    "G239","G231","G119","R1","R2","R3","R4","R9",
    "55220","55221","55222","55223","55224","55225","55226","55240","55241"
};

var output = new List<object>();

using var document = PdfDocument.Open(pdfPath);

for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
{
    Page page = document.GetPage(pageNumber);
    var words = page.GetWords().ToList();

    var containsInteresting = words.Any(w => interestingTokens.Contains(w.Text));
    if (!containsInteresting)
        continue;

    if (reportOnly && pagesFilter.Any() && !pagesFilter.Contains(pageNumber))
        continue;

    // Collect measurement-ish tokens with bounding boxes.
    var measurements = words
        .Where(w => IsProbablyMeasurementToken(w.Text))
        .Select(w => new
        {
            Text = w.Text,
            // PdfPig uses PDF coordinate system (origin bottom-left)
            X = Math.Round(w.BoundingBox.Left, 2),
            Y = Math.Round(w.BoundingBox.Bottom, 2),
            W = Math.Round(w.BoundingBox.Width, 2),
            H = Math.Round(w.BoundingBox.Height, 2)
        })
        .DistinctBy(m => new { m.Text, m.X, m.Y })
        .Take(400)
        .ToArray();

    var hits = words
        .Where(w => interestingTokens.Contains(w.Text))
        .Select(w => w.Text)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var text = page.Text ?? string.Empty;
    if (text.Length > 2000)
        text = text[..2000];

    output.Add(new
    {
        Page = pageNumber,
        Hits = hits,
        TextPreview = text,
        Measurements = measurements
    });

    if (reportOnly)
    {
        Console.WriteLine($"=== Page {pageNumber} ===");
        Console.WriteLine($"Hits: {string.Join(", ", hits)}");
        Console.WriteLine("Measurement candidates (Top 200, sorted by Y desc, X asc):");

        foreach (var m in measurements
                     .OrderByDescending(m => m.Y)
                     .ThenBy(m => m.X)
                     .Take(200))
        {
            Console.WriteLine($"  {m.Text} @ ({m.X},{m.Y}) {m.W}x{m.H}");
        }
    }
}

if (!reportOnly)
{
    var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    var outFile = Path.Combine(Path.GetDirectoryName(pdfPath)!, "piko-a-gleis.pdf.geometry-scan.json");
    File.WriteAllText(outFile, json);
    Console.WriteLine($"Wrote: {outFile}");
}

return 0;
