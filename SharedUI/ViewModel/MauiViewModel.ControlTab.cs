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
    private IReadOnlyList<LocomotiveFleetSnapshot>? _lastPublishedLocomotiveFleet;
    private Guid? _shownProjectId;

    private void WireProjectContextForControlTab()
    {
        if (_projectContext == null)
        {
            return;
        }

        _projectContext.PropertyChanged += OnProjectContextPropertyChangedForControlTab;
        RefreshProjectPicker();
        Z21IpAddress = _projectContext.SelectedProject?.Model.Z21.IpAddress.Trim() ?? string.Empty;
        _z21Port = _projectContext.SelectedProject?.Model.Z21.Port is > 0
            ? _projectContext.SelectedProject.Model.Z21.Port
            : Backend.Protocol.Z21Protocol.DefaultPort;
        _shownProjectId = _projectContext.SelectedProject?.Model.Id;
        _localProjectId = _runtimeSnapshots.Current.ProjectId == Guid.Empty ? null : _runtimeSnapshots.Current.ProjectId;
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

        if (!_runtimeProjection.HasAnyLocomotiveFleetAvailable)
        {
            RunInBackground(RequestSolutionSyncAsync(_applicationLifetimeCts.Token), "Solution sync fallback for Control tab fleet");
        }
    }

    /// <summary>
    /// Restores cached locomotive fleet loaded from local storage at app startup.
    /// </summary>
    public void RestoreCachedLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        _runtimeProjection.RestoreCachedLocomotiveFleet(fleet);
    }

    /// <summary>
    /// Returns the best locomotive fleet available immediately after a mobile cache restore.
    /// </summary>
    public IReadOnlyList<LocomotiveFleetSnapshot> GetStartupLocomotiveFleet() =>
        _runtimeProjection.GetBestAvailableLocomotiveFleet();

    /// <summary>
    /// Publishes the best currently available locomotive fleet to TrainControlViewModel.
    /// </summary>
    public void ApplyBestAvailableLocomotiveFleet()
    {
        var fleet = _pendingLocomotiveFleet is { Count: > 0 } pending
            ? pending
            : _runtimeProjection.GetBestAvailableLocomotiveFleet();
        _pendingLocomotiveFleet = null;
        PublishFleetUpdate(fleet);
    }

    private void PublishFleetUpdate(IReadOnlyList<LocomotiveFleetSnapshot> fleet, bool publishEmpty = false)
    {
        if (fleet.Count == 0 && !publishEmpty)
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

    private void CacheRemoteLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet) =>
        RunInBackground(
            _runtimeProjection.CacheRemoteLocomotiveFleet(fleet, _applicationLifetimeCts.Token),
            "Persist mobile locomotive fleet cache");

    private void ClearCachedRemoteLocomotiveFleet()
    {
        _runtimeProjection.ClearRemoteLocomotiveFleet();
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

        var fleet = _runtimeProjection.ResolveAuthoritativeLocomotiveFleet(remoteFleet);
        if (fleet.Count == 0)
        {
            return;
        }

        CacheRemoteLocomotiveFleet(fleet);
        PublishFleetUpdate(fleet);
    }

    private void RequestLocomotiveFleetSnapshotRefreshIfEmpty()
    {
        if (!_runtimeProjection.HasAnyLocomotiveFleetAvailable)
        {
            RequestSignalBoxSnapshotRefresh();
        }
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

        var projectId = _projectContext?.SelectedProject?.Model.Id;
        if (_shownProjectId is { } previous && previous != projectId)
        {
            // Locomotives and signals of the previous project must not stay controllable under the new project id.
            ClearCachedRemoteLocomotiveFleet();
            ClearCachedRemoteSignalBoxElements();
            _uiDispatcher.InvokeOnUi(() =>
            {
                ClearSignalBoxElements();
                PublishFleetUpdate(_runtimeProjection.GetBestAvailableLocomotiveFleet(), publishEmpty: true);
            });
        }
        else
        {
            _uiDispatcher.InvokeOnUi(ApplyBestAvailableLocomotiveFleet);
        }

        _shownProjectId = projectId;
        _uiDispatcher.InvokeOnUi(RefreshProjectPicker);
        RunInBackground(SelectRemoteProjectAsync(), "Select project and its Z21");
    }

    /// <summary>
    /// MOBAflow runs one runtime per project; remote control shows and commands the project selected here.
    /// </summary>
    private Task SelectRemoteProjectAsync() => QueueProjectSelection();
}
