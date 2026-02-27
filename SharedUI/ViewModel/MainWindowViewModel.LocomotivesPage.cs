// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;
using Domain;

/// <summary>
/// MainWindowViewModel – extension for LocomotivesPage and trip log.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Filtered trip log entries for the currently selected locomotive.
    /// </summary>
    public ObservableCollection<TripLogEntry> TripLogEntriesForSelectedLocomotive { get; } = [];

    /// <summary>
    /// true when a locomotive is selected and no trips have been recorded.
    /// </summary>
    public bool ShowEmptyTripLogMessage =>
        SelectedLocomotive != null && TripLogEntriesForSelectedLocomotive.Count == 0;

    /// <summary>
    /// Updates the trip log list for the selected locomotive.
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
