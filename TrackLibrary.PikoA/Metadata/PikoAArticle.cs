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

    // Geraden
    public static PikoAArticle G940 => new("55209", "G940", "Gerade 940mm", 7.99m, "Gerade");
    public static PikoAArticle G239 => new("55205", "G239", "Gerade 239mm", 4.99m, "Gerade");
    public static PikoAArticle G231 => new("55200", "G231", "Gerade 231mm", 4.99m, "Gerade");
    public static PikoAArticle G119 => new("55201", "G119", "Gerade 119mm", 3.99m, "Gerade");
    public static PikoAArticle G115 => new("55206", "G115", "Gerade 115mm", 3.99m, "Gerade");
    public static PikoAArticle G107 => new("55207", "G107", "Gerade 107mm", 3.79m, "Gerade");
    public static PikoAArticle G62 => new("55202", "G62", "Gerade 62mm", 3.49m, "Gerade");
    public static PikoAArticle G56 => new("55203", "G56", "Gerade 56mm", 3.49m, "Gerade");
    public static PikoAArticle G31 => new("55204", "G31", "Gerade 31mm", 2.99m, "Gerade");
    public static PikoAArticle G55620 => new("55620", "55620", "Anschlussgleis 37.5mm", 2.49m, "Anschluss");
    public static PikoAArticle G55621 => new("55621", "55621", "Anschlussgleis 77.5mm", 3.49m, "Anschluss");

    // Bögen
    public static PikoAArticle R1 => new("55211", "R1", "Bogen R1 30 Grad", 4.99m, "Bogen");
    public static PikoAArticle R2 => new("55212", "R2", "Bogen R2 30 Grad", 5.49m, "Bogen");
    public static PikoAArticle R3 => new("55213", "R3", "Bogen R3 30 Grad", 5.99m, "Bogen");
    public static PikoAArticle R4 => new("55214", "R4", "Bogen R4 30 Grad", 6.49m, "Bogen");
    public static PikoAArticle R9 => new("55219", "R9", "Bogen R9 15 Grad", 6.49m, "Bogen");
    public static PikoAArticle R1X => new("55290", "R1X", "Prellbock R1", 4.99m, "Prellbock");
    public static PikoAArticle R2X => new("55291", "R2X", "Prellbock R2", 5.49m, "Prellbock");

    // Weichen
    public static PikoAArticle WL => new("55420", "WL", "Weiche links", 24.99m, "Weiche");
    public static PikoAArticle WR => new("55421", "WR", "Weiche rechts", 24.99m, "Weiche");
    public static PikoAArticle BWL => new("55220", "BWL", "Bogenweiche links", 29.99m, "Weiche");
    public static PikoAArticle BWR => new("55221", "BWR", "Bogenweiche rechts", 29.99m, "Weiche");
    public static PikoAArticle BWL_R3 => new("55222", "BWL R3", "Bogenweiche links R3", 32.99m, "Weiche");
    public static PikoAArticle BWR_R3 => new("55223", "BWR R3", "Bogenweiche rechts R3", 32.99m, "Weiche");
    public static PikoAArticle W3 => new("55224", "W3", "Dreiwegweiche", 39.99m, "Weiche");

    // Kreuzungen
    public static PikoAArticle K30 => new("55240", "K30", "Kreuzung 30 Grad", 19.99m, "Kreuzung");
    public static PikoAArticle K10 => new("55245", "K10", "Kreuzung 10 Grad", 19.99m, "Kreuzung");

    // Doppelkreuzungsweichen
    public static PikoAArticle DKW => new("55241", "DKW", "Doppelkreuzungsweiche", 49.99m, "DKW");
    public static PikoAArticle DKW3 => new("55242", "DKW3", "Doppelkreuzungsweiche 3", 54.99m, "DKW");
}
