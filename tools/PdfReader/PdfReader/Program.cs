using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var pdfPaths = new[]
{
    @"C:\Repo\ahuelsmann\MOBAflow\docs\01825-29638_50311_CabControl_ESUKG_DE_Betriebsanleitung_Auflage_1_Mai_2025_eBook.pdf",
    @"C:\Repo\ahuelsmann\MOBAflow\docs\6f8a8c2e68227a0c4250d0678631c1f51434542196.pdf"
};

var outputDir = @"C:\Repo\ahuelsmann\MOBAflow\docs\extracted";
Directory.CreateDirectory(outputDir);

using var docReader = DocLib.Instance;

foreach (var pdfPath in pdfPaths)
{
    if (!File.Exists(pdfPath))
    {
        Console.WriteLine($"[FEHLT] {pdfPath}");
        continue;
    }

    var fileName = Path.GetFileNameWithoutExtension(pdfPath);
    var pdfOutputDir = Path.Combine(outputDir, fileName);
    Directory.CreateDirectory(pdfOutputDir);

    Console.WriteLine($"\n{'=',-80}");
    Console.WriteLine($"PDF: {Path.GetFileName(pdfPath)}");
    Console.WriteLine($"{'=',-80}");

    try
    {
        using var doc = docReader.GetDocReader(pdfPath, new PageDimensions(1440, 2160));
        var pageCount = doc.GetPageCount();
        Console.WriteLine($"Seiten: {pageCount}");

        // Nur erste 10 Seiten fuer Text + alle Seiten als Bilder
        var textFile = Path.Combine(pdfOutputDir, "text_content.md");
        using var textWriter = new StreamWriter(textFile);
        textWriter.WriteLine($"# {fileName}");
        textWriter.WriteLine($"Extrahiert am: {DateTime.Now:yyyy-MM-dd HH:mm}");
        textWriter.WriteLine();

        for (int i = 0; i < pageCount; i++)
        {
            using var page = doc.GetPageReader(i);
            var text = page.GetText();
            var width = page.GetPageWidth();
            var height = page.GetPageHeight();

            Console.WriteLine($"  Seite {i + 1}/{pageCount}: {width}x{height}px, {text.Length} Zeichen");

            // Text speichern (nur erste 15 Seiten)
            if (i < 15)
            {
                textWriter.WriteLine($"## Seite {i + 1}");
                textWriter.WriteLine();
                textWriter.WriteLine(text);
                textWriter.WriteLine();
                textWriter.WriteLine("---");
                textWriter.WriteLine();
            }

            // Bild speichern (nur erste 20 Seiten und jede 5. danach)
            if (i < 20 || i % 5 == 0)
            {
                var rawBytes = page.GetImage();
                if (rawBytes != null && rawBytes.Length > 0)
                {
                    var imageFile = Path.Combine(pdfOutputDir, $"page_{i + 1:D3}.png");
                    SaveRawBytesAsPng(rawBytes, width, height, imageFile);
                }
            }
        }

        Console.WriteLine($"  --> Text gespeichert: {textFile}");
        Console.WriteLine($"  --> Bilder gespeichert: {pdfOutputDir}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FEHLER] {ex.Message}");
    }
}

Console.WriteLine($"\n{'=',-80}");
Console.WriteLine("FERTIG! Extrahierte Dateien in:");
Console.WriteLine(outputDir);
Console.WriteLine($"{'=',-80}");

static void SaveRawBytesAsPng(byte[] rawBytes, int width, int height, string outputPath)
{
    // Docnet returns BGRA raw bytes
    var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
    using var bitmap = new SKBitmap(info);

    unsafe
    {
        fixed (byte* ptr = rawBytes)
        {
            bitmap.InstallPixels(info, (IntPtr)ptr, info.RowBytes);
        }
    }

    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 90);
    using var stream = File.OpenWrite(outputPath);
    data.SaveTo(stream);
}
