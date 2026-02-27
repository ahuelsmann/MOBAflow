namespace Moba.TrackLibrary.PikoA;
/// <summary>
/// Katalog aller Piko A-Gleistypen für Toolbox und Drag &amp; Drop.
/// Gruppiert nach Kategorien für die TrackPlanPage.
/// Ermöglicht spätere Erweiterung um weitere Gleisbibliotheken (z.B. Märklin).
/// </summary>
public static class PikoACatalog
{
    /// <summary>
    /// Kategorie eines Gleistyps (für Toolbox-Gruppierung).
    /// </summary>
    public enum TrackCategory
    {
        /// <summary>
        /// Straight track elements (Geraden).
        /// </summary>
        Straight,

        /// <summary>
        /// Curved track elements (Bögen).
        /// </summary>
        Curve,

        /// <summary>
        /// Turnouts and switches (Weichen).
        /// </summary>
        Switch,

        /// <summary>
        /// Crossing track elements (Kreuzungen).
        /// </summary>
        Crossing
    }

    /// <summary>
    /// Alle Geraden (Straight) im Piko A-Sortiment.
    /// </summary>
    public static IReadOnlyList<TrackCatalogEntry> Straights { get; } =
    [
        new TrackCatalogEntry("G62", "G62 (61,88 mm)", TrackCategory.Straight, typeof(G62)),
        new TrackCatalogEntry("G107", "G107 (107,32 mm)", TrackCategory.Straight, typeof(G107)),
        new TrackCatalogEntry("G115", "G115 (115,46 mm)", TrackCategory.Straight, typeof(G115)),
        new TrackCatalogEntry("G119", "G119 (119,54 mm)", TrackCategory.Straight, typeof(G119)),
        new TrackCatalogEntry("G231", "G231 (230,93 mm)", TrackCategory.Straight, typeof(G231)),
        new TrackCatalogEntry("G239", "G239 (239,07 mm)", TrackCategory.Straight, typeof(G239))
    ];

    /// <summary>
    /// Alle Kurven (Curved) im Piko A-Sortiment.
    /// </summary>
    public static IReadOnlyList<TrackCatalogEntry> Curves { get; } =
    [
        new TrackCatalogEntry("R1", "R1 (360 mm / 30°)", TrackCategory.Curve, typeof(R1)),
        new TrackCatalogEntry("R2", "R2 (422 mm / 30°)", TrackCategory.Curve, typeof(R2)),
        new TrackCatalogEntry("R3", "R3 (484 mm / 30°)", TrackCategory.Curve, typeof(R3)),
        new TrackCatalogEntry("R4", "R4 (546 mm / 30°)", TrackCategory.Curve, typeof(R4)),
        new TrackCatalogEntry("R9", "R9 (908 mm / 15°)", TrackCategory.Curve, typeof(R9)),
        new TrackCatalogEntry("R175", "R1/75 (360 mm / 7,5°)", TrackCategory.Curve, typeof(R175)),
        new TrackCatalogEntry("R275", "R2/75 (422 mm / 7,5°)", TrackCategory.Curve, typeof(R275))
    ];

    /// <summary>
    /// Alle Weichen im Piko A-Sortiment.
    /// </summary>
    public static IReadOnlyList<TrackCatalogEntry> Switches { get; } =
    [
        new TrackCatalogEntry("WR", "WR Rechtsweiche 15°", TrackCategory.Switch, typeof(WR)),
        new TrackCatalogEntry("WL", "WL Linksweiche 15°", TrackCategory.Switch, typeof(WL)),
        new TrackCatalogEntry("WY", "WY Y-Weiche 30°", TrackCategory.Switch, typeof(WY)),
        new TrackCatalogEntry("W3", "W3 Dreiwegeweiche", TrackCategory.Switch, typeof(W3)),
        new TrackCatalogEntry("BWL", "BWL Linkskurvenweiche", TrackCategory.Switch, typeof(BWL)),
        new TrackCatalogEntry("BWR", "BWR Rechtskurvenweiche", TrackCategory.Switch, typeof(BWR)),
        new TrackCatalogEntry("BWLR3", "BWLR3 Links R3→R4", TrackCategory.Switch, typeof(BWLR3)),
        new TrackCatalogEntry("BWRR3", "BWRR3 Rechts R3→R4", TrackCategory.Switch, typeof(BWRR3)),
        new TrackCatalogEntry("DKW", "DKW Doppelkreuzungsweiche", TrackCategory.Switch, typeof(DKW))
    ];

    /// <summary>
    /// Alle Kreuzungen im Piko A-Sortiment.
    /// </summary>
    public static IReadOnlyList<TrackCatalogEntry> Crossings { get; } =
    [
        new TrackCatalogEntry("K15", "K15 Kreuzung 15°", TrackCategory.Crossing, typeof(K15)),
        new TrackCatalogEntry("K30", "K30 Kreuzung 30°", TrackCategory.Crossing, typeof(K30))
    ];

    /// <summary>
    /// Alle Gleistypen in einer flachen Liste.
    /// </summary>
    public static IReadOnlyList<TrackCatalogEntry> All { get; } =
    [
        .. Straights,
        .. Curves,
        .. Switches,
        .. Crossings
    ];
}
