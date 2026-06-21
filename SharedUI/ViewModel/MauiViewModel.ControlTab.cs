// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;
using Common.Runtime;

using Domain;

using Interface;

using System.ComponentModel;

/// <summary>
/// MOBAsmart Control tab fleet coordination — resolves fleet sources and publishes updates for TrainControlViewModel.
/// </summary>
public sealed partial class MauiViewModel
{
    private readonly IProjectContext? _projectContext;
    private bool _controlTabActive;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _pendingLocomotiveFleet;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _cachedRemoteLocomotiveFleet;

    private void WireProjectContextForControlTab()
    {
        if (_projectContext == null)
        {
            return;
        }

        _projectContext.PropertyChanged += OnProjectContextPropertyChangedForControlTab;
    }

    /// <summary>
    /// Tracks whether the Control tab is visible so fleet updates can be deferred like SignalBox.
    /// </summary>
    public void SetControlTabActive(bool isActive)
    {
        _controlTabActive = isActive;

        if (!isActive)
        {
            return;
        }

        ApplyBestAvailableLocomotiveFleet();
        RequestLocomotiveFleetSnapshotRefreshIfEmpty();

        if (!HasAnyLocomotiveFleetAvailable())
        {
            RunInBackground(RequestSolutionSyncAsync(), "Solution sync fallback for Control tab fleet");
        }
    }

    /// <summary>
    /// Restores cached locomotive fleet loaded from local storage at app startup.
    /// </summary>
    public void RestoreCachedLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        if (fleet.Count == 0)
        {
            return;
        }

        _cachedRemoteLocomotiveFleet = fleet;
    }

    /// <summary>
    /// Returns the best locomotive fleet available immediately after a mobile cache restore.
    /// </summary>
    public IReadOnlyList<LocomotiveFleetSnapshot> GetStartupLocomotiveFleet() =>
        GetBestAvailableLocomotiveFleet();

    /// <summary>
    /// Publishes the best currently available locomotive fleet to TrainControlViewModel.
    /// </summary>
    public void ApplyBestAvailableLocomotiveFleet()
    {
        var fleet = _pendingLocomotiveFleet is { Count: > 0 } pending
            ? pending
            : GetBestAvailableLocomotiveFleet();
        _pendingLocomotiveFleet = null;
        PublishFleetUpdate(fleet);
    }

    private void PublishFleetUpdate(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        if (fleet.Count == 0)
        {
            return;
        }

        if (!_controlTabActive)
        {
            _pendingLocomotiveFleet = fleet;
            return;
        }

        _eventBus.Publish(new LocomotiveFleetUpdatedEvent(fleet));
    }

    private void CacheRemoteLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        if (fleet.Count == 0)
        {
            return;
        }

        _cachedRemoteLocomotiveFleet = fleet;

        if (_mobileSolutionStore != null)
        {
            RunInBackground(
                _mobileSolutionStore.SaveLocomotiveFleetAsync(fleet, _applicationLifetimeCts.Token),
                "Persist mobile locomotive fleet cache");
        }
    }

    private IReadOnlyList<LocomotiveFleetSnapshot> GetBestAvailableLocomotiveFleet()
    {
        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true
            && _cachedRemoteLocomotiveFleet is { Count: > 0 })
        {
            return _cachedRemoteLocomotiveFleet;
        }

        if (_cachedRemoteLocomotiveFleet is { Count: > 0 })
        {
            var local = _mobaRuntime.Current.LocomotiveFleet;
            if (local.Count > 0)
            {
                return local;
            }

            return _cachedRemoteLocomotiveFleet;
        }

        var localFleet = _mobaRuntime.Current.LocomotiveFleet;
        if (localFleet.Count > 0)
        {
            return localFleet;
        }

        var projectFleet = BuildFleetSnapshotsFromProjectContext();
        if (projectFleet.Count > 0)
        {
            return projectFleet;
        }

        return _cachedRemoteLocomotiveFleet ?? [];
    }

    private void RequestLocomotiveFleetSnapshotRefreshIfEmpty()
    {
        if (!HasAnyLocomotiveFleetAvailable())
        {
            RequestSignalBoxSnapshotRefresh();
        }
    }

    private bool HasAnyLocomotiveFleetAvailable()
    {
        if (_cachedRemoteLocomotiveFleet is { Count: > 0 })
        {
            return true;
        }

        if (_mobaRuntime.Current.LocomotiveFleet.Count > 0)
        {
            return true;
        }

        return (_projectContext?.SelectedProject?.Locomotives.Count ?? 0) > 0;
    }

    private IReadOnlyList<LocomotiveFleetSnapshot> BuildFleetSnapshotsFromProjectContext()
    {
        var project = _projectContext?.SelectedProject;
        if (project == null)
        {
            return [];
        }

        return project.Locomotives
            .Select(loco => new LocomotiveFleetSnapshot
            {
                LocomotiveId = loco.Model.Id,
                Name = loco.Name,
                DigitalAddress = loco.DigitalAddress,
                PhotoPath = loco.PhotoPath
            })
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private void OnSolutionSyncedForControlTab(SolutionSyncedEvent _)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            ApplyBestAvailableLocomotiveFleet();

            if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true)
            {
                RequestSignalBoxSnapshotRefresh();
            }
        });
    }

    private void OnProjectContextPropertyChangedForControlTab(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(IProjectContext.SelectedProject) or nameof(IProjectContext.SolutionViewModel)))
        {
            return;
        }

        _uiDispatcher.InvokeOnUi(ApplyBestAvailableLocomotiveFleet);
    }
}
