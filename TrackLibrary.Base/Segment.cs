namespace Moba.TrackLibrary.Base;

/// <summary>
/// Basis für alle Gleissegmente.
/// Records ermöglichen Wert-Semantik und immutable Geometrie bei erweiterbarer Verbindungslogik.
/// </summary>
public abstract record Segment
{
    /// <summary>
    /// Unique identifier of the segment in the track plan.
    /// </summary>
    public Guid No { get; set; } = Guid.NewGuid();
}
