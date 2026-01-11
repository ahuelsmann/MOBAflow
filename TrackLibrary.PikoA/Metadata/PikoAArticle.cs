// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackLibrary.PikoA.Metadata;

/// <summary>
/// Article metadata for a Piko A-Gleis track piece.
/// Contains product information for catalog display.
/// </summary>
public sealed record PikoAArticle(
    string ArticleNumber,
    string Name,
    string Description,
    decimal Price,
    string Category
)
{
    /// <summary>Article number prefix for Piko A-Gleis</summary>
    public const string ArticlePrefix = "55";

    public static PikoAArticle G231 => new("55200", "G231", "Gerade 231mm", 4.99m, "Gerade");
    public static PikoAArticle G119 => new("55201", "G119", "Gerade 119mm", 3.99m, "Gerade");
    public static PikoAArticle G62 => new("55202", "G62", "Gerade 62mm", 3.49m, "Gerade");
    public static PikoAArticle G56 => new("55203", "G56", "Gerade 56mm", 3.49m, "Gerade");
    public static PikoAArticle G31 => new("55204", "G31", "Gerade 31mm", 2.99m, "Gerade");

    public static PikoAArticle R1 => new("55211", "R1", "Bogen R1 30 Grad", 4.99m, "Bogen");
    public static PikoAArticle R2 => new("55212", "R2", "Bogen R2 30 Grad", 5.49m, "Bogen");
    public static PikoAArticle R3 => new("55213", "R3", "Bogen R3 30 Grad", 5.99m, "Bogen");
    public static PikoAArticle R9 => new("55219", "R9", "Bogen R9 15 Grad", 6.49m, "Bogen");

    public static PikoAArticle BWL => new("55220", "BWL", "Weiche links", 29.99m, "Weiche");
    public static PikoAArticle BWR => new("55221", "BWR", "Weiche rechts", 29.99m, "Weiche");
    public static PikoAArticle K30 => new("55240", "K30", "Kreuzung 30 Grad", 19.99m, "Kreuzung");
}
