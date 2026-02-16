// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Ein Eintrag im Fahrtenbuch – erfasst eine Fahrt- oder Haltephase einer Lokomotive
/// über die TrainControlPage.
/// </summary>
public class TripLogEntry
{
    public TripLogEntry()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }

    /// <summary>
    /// DCC-Adresse der Lokomotive (1–9999).
    /// </summary>
    public int LocoAddress { get; set; }

    /// <summary>
    /// Startzeit der Phase (Fahrt oder Halt).
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Endzeit der Phase. Null = Phase läuft noch.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Geschwindigkeit (0–126 für 128 Speed Steps). 0 = Haltezeit.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// true = Haltezeit (Speed war 0), false = Fahrtphase.
    /// </summary>
    public bool IsStopSegment { get; set; }

    /// <summary>
    /// Dauer der Phase. Null wenn EndTime noch nicht gesetzt (laufende Phase).
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}
