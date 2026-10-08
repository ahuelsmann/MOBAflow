// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using System.ComponentModel;

/// <summary>
/// MainWindowViewModel — page-level auto-save tracking and the shell's reactions to persisted model changes.
/// The solution session owns the auto-save observer.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Called when SelectedJourney changes. The session tracks the journey for auto-save.
    /// </summary>
    private void HandleSelectedJourneyChanged()
    {
        ResetJourneyCommand.NotifyCanExecuteChanged();
        ResetJourneyCounterCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when SelectedStation changes. Tracks the station for auto-save.
    /// </summary>
    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            _session.TrackChanges(value);
        }
    }

    /// <summary>
    /// Shell reaction to a persisted model change reported by the solution session, before it saves.
    /// The session already refreshes the runtime's project snapshot.
    /// </summary>
    private void OnSolutionModelChanged(object? sender, PropertyChangedEventArgs e) => RefreshProjectDiagnostics();
}
