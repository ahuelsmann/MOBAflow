namespace Moba.TrackLibrary.Base;

/// <summary>
/// Basis für alle Gleissegmente.
/// Records ermöglichen Wert-Semantik und immutable Geometrie bei erweiterbarer Verbindungslogik.
/// </summary>
public abstract record Segment
{
    public Guid No { get; set; } = Guid.NewGuid();
}
