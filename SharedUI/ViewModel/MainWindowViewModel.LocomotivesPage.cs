// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;
using Domain;

/// <summary>
/// MainWindowViewModel – Erweiterung für LocomotivesPage und Fahrtenbuch.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Gefilterte Fahrtenbuch-Einträge für die aktuell gewählte Lokomotive.
    /// </summary>
    public ObservableCollection<TripLogEntry> TripLogEntriesForSelectedLocomotive { get; } = [];

    /// <summary>
    /// true wenn eine Lok ausgewählt ist und keine Fahrten protokolliert sind.
    /// </summary>
    public bool ShowEmptyTripLogMessage =>
        SelectedLocomotive != null && TripLogEntriesForSelectedLocomotive.Count == 0;

    /// <summary>
    /// Aktualisiert die Fahrtenbuch-Liste für die gewählte Lokomotive.
    /// </summary>
    public void RefreshTripLogEntriesForSelectedLocomotive()
    {
        var entries = TripLogEntriesForSelectedLocomotive;
        entries.Clear();

        var loco = SelectedLocomotive;
        var project = SelectedProject?.Model;

        if (loco?.DigitalAddress == null || project?.TripLogEntries == null)
        {
            return;
        }

        var address = (int)loco.DigitalAddress.Value;
        var filtered = project.TripLogEntries
            .Where(e => e.LocoAddress == address)
            .OrderByDescending(e => e.StartTime)
            .ToList();

        foreach (var e in filtered)
        {
            entries.Add(e);
        }
    }
}
