// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;
using Common.Runtime;

using Domain;

using Interface;

using System.ComponentModel;

/// <summary>
/// MOBAsmart Engines/Control tab fleet coordination — resolves fleet sources and publishes updates for TrainControlViewModel.
/// </summary>
public sealed partial class MauiViewModel
{
    private readonly IProjectContext? _projectContext;
    private bool _controlTabActive;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _pendingLocomotiveFleet;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _cachedRemoteLocomotiveFleet;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _lastPublishedLocomotiveFleet;
    private bool _remoteLocomotiveFleetIsLive;

    private void WireProjectContextForControlTab()
    {
        if (_projectContext == null)
        {
            return;
        }

        _projectContext.PropertyChanged += OnProjectContextPropertyChangedForControlTab;
    }

    /// <summary>
    /// Tracks whether the Engines or Control tab is visible so fleet updates can be deferred like SignalBox.
    /// </summary>
    public void SetControlTabActive(bool isActive)
    {
        _controlTabActive = isActive;

        if (!isActive)
        {
            return;
        }

        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true)
        {
            RequestSignalBoxSnapshotRefresh();
        }
        else
        {
            RequestLocomotiveFleetSnapshotRefreshIfEmpty();
        }

        ApplyBestAvailableLocomotiveFleet();

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
        _remoteLocomotiveFleetIsLive = false;
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

        var orderedFleet = fleet
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        if (_lastPublishedLocomotiveFleet is not null
            && LocomotiveFleetSnapshotComparer.OrderedContentEquals(orderedFleet, _lastPublishedLocomotiveFleet))
        {
            return;
        }

        if (!_controlTabActive)
        {
            _pendingLocomotiveFleet = orderedFleet;
            return;
        }

        _lastPublishedLocomotiveFleet = orderedFleet;
        _eventBus.Publish(new LocomotiveFleetUpdatedEvent(orderedFleet));
    }

    private void CacheRemoteLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        if (fleet.Count == 0)
        {
            return;
        }

        _cachedRemoteLocomotiveFleet = fleet;
        _remoteLocomotiveFleetIsLive = true;

        if (_mobileSolutionStore != null)
        {
            RunInBackground(
                _mobileSolutionStore.SaveLocomotiveFleetAsync(fleet, _applicationLifetimeCts.Token),
                "Persist mobile locomotive fleet cache");
        }
    }

    private void ClearCachedRemoteLocomotiveFleet()
    {
        _cachedRemoteLocomotiveFleet = null;
        _remoteLocomotiveFleetIsLive = false;
        _pendingLocomotiveFleet = null;
        _lastPublishedLocomotiveFleet = null;
    }

    /// <summary>
    /// Applies a live locomotive fleet snapshot received from MOBAflow, replacing stale mobile cache data.
    /// </summary>
    internal void ApplyLiveRemoteLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> remoteFleet)
    {
        if (remoteFleet.Count == 0)
        {
            return;
        }

        var fleet = ResolveAuthoritativeLocomotiveFleet(remoteFleet);
        if (fleet.Count == 0)
        {
            return;
        }

        CacheRemoteLocomotiveFleet(fleet);
        PublishFleetUpdate(fleet);
    }

    private IReadOnlyList<LocomotiveFleetSnapshot> ResolveAuthoritativeLocomotiveFleet(
        IReadOnlyList<LocomotiveFleetSnapshot> remoteFleet)
    {
        var filtered = FilterFleetToCurrentProject(remoteFleet);
        if (filtered.Count == 0)
        {
            return [];
        }

        var projectFleet = BuildFleetSnapshotsFromProjectContext();
        if (projectFleet.Count == 0)
        {
            return filtered
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        var projectIds = projectFleet.Select(item => item.LocomotiveId).ToHashSet();
        if (filtered.Count >= projectFleet.Count
            && filtered.All(item => projectIds.Contains(item.LocomotiveId)))
        {
            return filtered
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        var remoteById = filtered.ToDictionary(item => item.LocomotiveId);
        return projectFleet
            .Select(item => remoteById.TryGetValue(item.LocomotiveId, out var remote)
                ? remote
                : item)
            .ToList();
    }

    private IReadOnlyList<LocomotiveFleetSnapshot> FilterFleetToCurrentProject(
        IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        var project = _projectContext?.SelectedProject?.Model;
        if (project == null || fleet.Count == 0)
        {
            return fleet;
        }

        var projectIds = project.Locomotives.Select(loco => loco.Id).ToHashSet();
        return fleet
            .Where(item => projectIds.Contains(item.LocomotiveId))
            .ToList();
    }

    private bool ShouldCacheRemoteLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> remoteFleet)
    {
        var projectFleet = BuildFleetSnapshotsFromProjectContext();
        if (projectFleet.Count == 0)
        {
            return true;
        }

        var projectIds = projectFleet.Select(item => item.LocomotiveId).ToHashSet();
        return remoteFleet.Count >= projectFleet.Count
               && remoteFleet.All(item => projectIds.Contains(item.LocomotiveId));
    }

    private IReadOnlyList<LocomotiveFleetSnapshot> GetBestAvailableLocomotiveFleet()
    {
        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true)
        {
            if (_remoteLocomotiveFleetIsLive
                && _cachedRemoteLocomotiveFleet is { Count: > 0 })
            {
                return _cachedRemoteLocomotiveFleet;
            }

            var liveRuntimeFleet = _mobaRuntime.Current.LocomotiveFleet;
            if (liveRuntimeFleet.Count > 0)
            {
                return liveRuntimeFleet;
            }
        }

        var projectFleet = BuildFleetSnapshotsFromProjectContext();
        if (projectFleet.Count > 0)
        {
            return projectFleet;
        }

        if (_cachedRemoteLocomotiveFleet is { Count: > 0 })
        {
            return _cachedRemoteLocomotiveFleet;
        }

        var localFleet = _mobaRuntime.Current.LocomotiveFleet;
        if (localFleet.Count > 0)
        {
            return localFleet;
        }

        return [];
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
