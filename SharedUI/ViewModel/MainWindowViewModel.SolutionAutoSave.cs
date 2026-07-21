// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Events;
using Common.Extension;
using Common.Runtime;

using Microsoft.Extensions.Logging;

using System.ComponentModel;
using System.Threading;

/// <summary>
/// MainWindowViewModel — solution file auto-save coordination (suppression counter, semaphore, property-change hook).
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// When greater than zero, PropertyChanged-driven solution auto-save is suppressed (bulk load / new solution).
    /// </summary>
    private int _solutionAutoSaveSuppressionCount;

    /// <summary>
    /// Ensures at most one solution file write runs at a time (avoids races on temp/rename writes).
    /// </summary>
    private readonly SemaphoreSlim _solutionSaveSemaphore = new(1, 1);

    /// <summary>
    /// Set to 1 after <see cref="DrainAndDisposeSolutionSaveSemaphoreAsync"/> has run (idempotent shutdown).
    /// </summary>
    private int _solutionSaveSemaphoreDrainStarted;

    /// <summary>
    /// Called when SelectedJourney changes. Subscribes to PropertyChanged for auto-save.
    /// </summary>
    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }

        ResetJourneyCommand.NotifyCanExecuteChanged();
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
        if (Volatile.Read(ref _solutionAutoSaveSuppressionCount) > 0)
        {
            return;
        }

        // Ignore UI-only or runtime-backed properties that must not persist the whole solution.
        if (e.PropertyName is { } name &&
            (name is "IsSelected" or "IsExpanded" or "IsHighlighted" or "IsCurrentStation"
             or "CurrentStation" or "CurrentStepOccurrence" or "CurrentPos"))
        {
            return;
        }

        RefreshProjectDiagnostics();
        SaveSolutionInternalAsync().Observe(ex => _logger.LogWarning(ex, "Auto-save solution failed"));
    }

    private void OnVehicleUsageCheckpointCommitted(VehicleUsageCheckpointCommittedEvent checkpoint)
    {
        if (SelectedProject?.Model.Id != checkpoint.ProjectId || _isShuttingDown)
        {
            return;
        }

        SynchronizeVehicleUsage(checkpoint.Usage);
        SaveSolutionInternalAsync().Observe(ex =>
            _logger.LogWarning(ex, "Persisting vehicle usage checkpoint to the solution failed"));
    }

    private void SynchronizeVehicleUsageFromRuntime() => SynchronizeVehicleUsage(_mobaRuntime.Current.VehicleUsage);

    private void SynchronizeVehicleUsage(IReadOnlyDictionary<Guid, VehicleUsageRuntimeSnapshot> runtimeUsage)
    {
        var project = SelectedProject?.Model;
        if (project == null)
        {
            return;
        }

        foreach (var (vehicleId, usageSnapshot) in runtimeUsage)
        {
            Domain.VehicleUsageData? usage = null;
            if (project.Locomotives.FirstOrDefault(vehicle => vehicle.Id == vehicleId) is { } locomotive)
            {
                usage = locomotive.Usage ??= new Domain.VehicleUsageData();
            }
            else if (project.PassengerWagons.FirstOrDefault(vehicle => vehicle.Id == vehicleId) is { } passengerWagon)
            {
                usage = passengerWagon.Usage ??= new Domain.VehicleUsageData();
            }
            else if (project.GoodsWagons.FirstOrDefault(vehicle => vehicle.Id == vehicleId) is { } goodsWagon)
            {
                usage = goodsWagon.Usage ??= new Domain.VehicleUsageData();
            }

            if (usage == null)
            {
                continue;
            }

            usage.TrackedOperatingSeconds = usageSnapshot.TrackedOperatingSeconds;
            usage.TrackedCompletedTrips = usageSnapshot.TrackedCompletedTrips;
        }
    }

    /// <summary>
    /// Increments suppression counter so <see cref="OnViewModelPropertyChanged"/> does not trigger auto-save.
    /// Must be paired with <see cref="EndSuppressSolutionAutoSave"/>.
    /// </summary>
    private void BeginSuppressSolutionAutoSave() => Interlocked.Increment(ref _solutionAutoSaveSuppressionCount);

    /// <summary>
    /// Decrements suppression counter started by <see cref="BeginSuppressSolutionAutoSave"/>.
    /// </summary>
    private void EndSuppressSolutionAutoSave() => Interlocked.Decrement(ref _solutionAutoSaveSuppressionCount);

    /// <summary>
    /// Waits until no in-flight solution save holds the semaphore, then releases managed resources.
    /// Safe to call once per application shutdown; subsequent calls are ignored.
    /// </summary>
    private async Task DrainAndDisposeSolutionSaveSemaphoreAsync()
    {
        if (Interlocked.CompareExchange(ref _solutionSaveSemaphoreDrainStarted, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await _solutionSaveSemaphore.WaitAsync().ConfigureAwait(false);
            _solutionSaveSemaphore.Release();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        _solutionSaveSemaphore.Dispose();
    }
}
