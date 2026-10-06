// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Extension;

using Microsoft.Extensions.Logging;

using System.ComponentModel;

/// <summary>
/// MainWindowViewModel — property-change hook that triggers solution auto-save through the solution session.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Called when SelectedJourney changes. Subscribes to PropertyChanged for auto-save.
    /// </summary>
    private void HandleSelectedJourneyChanged(JourneyViewModel? oldValue, JourneyViewModel? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= OnViewModelPropertyChanged;
        if (newValue != null)
        {
            newValue.PropertyChanged += OnViewModelPropertyChanged;
        }

        ResetJourneyCommand.NotifyCanExecuteChanged();
        ResetJourneyCounterCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when SelectedStation changes. Subscribes to PropertyChanged for auto-save.
    /// </summary>
    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// Generic handler for ViewModel PropertyChanged events.
    /// Triggers auto-save for any model property change.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_session.IsAutoSaveSuppressed)
        {
            return;
        }

        // Ignore UI-only or runtime-backed properties that must not persist the whole solution.
        if (e.PropertyName is { } name &&
            (name is "IsSelected" or "IsExpanded" or "IsHighlighted" or "IsCurrentStation"
             or "CurrentStation" or "CurrentPos"))
        {
            return;
        }

        // The runtime executes an isolated copy, so journey activation and event edits must be re-applied.
        if (sender is JourneyViewModel journey && SelectedProject is { } project
            && e.PropertyName is nameof(JourneyViewModel.IsActive) or nameof(JourneyViewModel.EventPlan))
        {
            ObserveBackgroundTask(_runtimeConnection.UpdateJourneyEventsAsync(project.Model, journey.Model.Id), "Update journey events");
        }

        RefreshProjectDiagnostics();
        SaveSolutionInternalAsync().Observe(ex => _logger.LogWarning(ex, "Auto-save solution failed"));
    }
}
