namespace Moba.TrackLibrary.PikoA;

using Base;

/// <summary>
/// Entry in the track catalog for toolbox and drag and drop.
/// </summary>
/// <param name="Code">Article number/code (e.g. "G119", "55202")</param>
/// <param name="DisplayName">Display name for toolbox</param>
/// <param name="Category">Category for grouping</param>
/// <param name="SegmentType">CLR type for instantiation</param>
public sealed record TrackCatalogEntry(string Code, string DisplayName, PikoACatalog.TrackCategory Category, Type SegmentType)
{
    /// <summary>
    /// Creates a new instance of the track type with a unique No.
    /// </summary>
    public Segment CreateInstance()
    {
        var instance = (Segment)Activator.CreateInstance(SegmentType)!;
        instance.No = Guid.NewGuid();
        return instance;
    }
}
