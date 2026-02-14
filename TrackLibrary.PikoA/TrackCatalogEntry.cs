namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Eintrag im Gleiskatalog für Toolbox und Drag &amp; Drop.
/// </summary>
/// <param name="Code">Artikelnummer/Code (z.B. "G119", "55202")</param>
/// <param name="DisplayName">Anzeigename für Toolbox</param>
/// <param name="Category">Kategorie für Gruppierung</param>
/// <param name="SegmentType">CLR-Typ für Instanziierung</param>
public sealed record TrackCatalogEntry(string Code, string DisplayName, PikoACatalog.TrackCategory Category, Type SegmentType)
{
    /// <summary>
    /// Erstellt eine neue Instanz des Gleistyps mit eindeutiger No.
    /// </summary>
    public Segment CreateInstance()
    {
        var instance = (Segment)Activator.CreateInstance(SegmentType)!;
        instance.No = Guid.NewGuid();
        return instance;
    }
}
