// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Domain;

/// <summary>
/// Service zum Protokollieren von Fahrten und Haltezeiten über die TrainControlPage.
/// Ruft RecordStateChange bei jeder Änderung von Geschwindigkeit oder DCC-Adresse auf.
/// </summary>
public interface ITripLogService
{
    /// <summary>
    /// Wird ausgelöst, wenn ein neuer Fahrtenbuch-Eintrag hinzugefügt wurde.
    /// Ermöglicht UI-Updates ohne Polling.
    /// </summary>
    event EventHandler? EntryAdded;

    /// <summary>
    /// Erfasst eine Zustandsänderung (Adresse oder Geschwindigkeit).
    /// Bei Projekt null wird nichts protokolliert.
    /// </summary>
    /// <param name="project">Aktuelles Projekt (null = kein Logging)</param>
    /// <param name="locoAddress">DCC-Adresse der Lokomotive</param>
    /// <param name="speed">Geschwindigkeit (0–126, 0 = Halt)</param>
    /// <param name="timestamp">Zeitpunkt der Änderung (UTC empfohlen)</param>
    void RecordStateChange(Project? project, int locoAddress, int speed, DateTime timestamp);
}
