// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Events;
using Common.Runtime;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Tab-aware update throttling for the MAUI mobile client.
/// </summary>
public sealed partial class MauiViewModel
{
    private bool _heavyUpdatesPaused;
    private bool _signalBoxTabActive;
    private IReadOnlyList<SignalBoxElementRuntimeSnapshot>? _pendingSignalBoxElements;
    private IReadOnlyList<SignalBoxElementRuntimeSnapshot>? _cachedRemoteSignalBoxElements;

    /// <summary>
    /// Suppresses non-critical snapshot work (e.g. signal-box list rebuild) while another tab is active.
    /// </summary>
    public void PauseHeavyUpdates() => _heavyUpdatesPaused = true;

    /// <summary>
    /// Re-enables full snapshot updates and applies any deferred signal-box state once.
    /// </summary>
    public void ResumeHeavyUpdates()
    {
        _heavyUpdatesPaused = false;

        if (!_signalBoxTabActive)
        {
            return;
        }

        ApplyBestAvailableSignalBoxElements();
        RequestSignalBoxSnapshotRefreshIfEmpty();
    }

    /// <summary>
    /// Tracks whether the SignalBox tab is visible so list rebuilds can be skipped on other tabs.
    /// </summary>
    public void SetSignalBoxTabActive(bool isActive)
    {
        _signalBoxTabActive = isActive;

        if (!isActive)
        {
            return;
        }

        if (!_heavyUpdatesPaused)
        {
            ApplyBestAvailableSignalBoxElements();
        }

        RequestSignalBoxSnapshotRefreshIfEmpty();
    }

    private void CacheRemoteSignalBoxElements(IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements)
    {
        if (elements.Count == 0)
        {
            return;
        }

        _cachedRemoteSignalBoxElements = elements;

        if (_mobileSolutionStore != null)
        {
            RunInBackground(
                _mobileSolutionStore.SaveSignalBoxElementsAsync(elements, _applicationLifetimeCts.Token),
                "Persist mobile signal-box cache");
        }
    }

    private void ClearCachedRemoteSignalBoxElements()
    {
        _cachedRemoteSignalBoxElements = null;
        _pendingSignalBoxElements = null;
    }

    private void ClearSignalBoxElements()
    {
        if (SignalBoxElements.Count == 0)
        {
            return;
        }

        DetachAllSignalBoxHandlers();
        SignalBoxElements.Clear();
        OnPropertyChanged(nameof(HasSignalBoxElements));
    }

    private IReadOnlyList<SignalBoxElementRuntimeSnapshot> GetBestAvailableSignalBoxElements()
    {
        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true
            && _cachedRemoteSignalBoxElements is { Count: > 0 })
        {
            return _cachedRemoteSignalBoxElements;
        }

        if (_cachedRemoteSignalBoxElements is { Count: > 0 })
        {
            var local = _mobaRuntime.Current.SignalBoxElements;
            if (local.Count > 0)
            {
                return SignalBoxSnapshotMerge.MergeAspectsFromCache(local, _cachedRemoteSignalBoxElements);
            }

            return _cachedRemoteSignalBoxElements;
        }

        return _mobaRuntime.Current.SignalBoxElements;
    }

    /// <summary>
    /// Restores cached signal-box and locomotive fleet data from local storage at app startup.
    /// </summary>
    public void RestoreCachedMobileSnapshot(MobileSolutionCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        RestoreCachedSignalBoxElements(entry.SignalBoxElements);
        RestoreCachedLocomotiveFleet(entry.LocomotiveFleet);
    }

    /// <summary>
    /// Applies restored mobile cache data to bound UI collections, bypassing tab-visibility gates
    /// used during normal runtime streaming.
    /// </summary>
    public void ApplyRestoredMobileCacheToUi()
    {
        var signalBoxElements = GetBestAvailableSignalBoxElements();
        if (signalBoxElements.Count > 0)
        {
            _pendingSignalBoxElements = null;
            RefreshSignalBoxElements(signalBoxElements, forceApply: true);
        }

        var fleet = GetBestAvailableLocomotiveFleet();
        if (fleet.Count > 0)
        {
            _pendingLocomotiveFleet = null;
            _eventBus.Publish(new LocomotiveFleetUpdatedEvent(fleet));
        }
    }

    /// <summary>
    /// Restores cached signal-box elements loaded from local storage at app startup.
    /// </summary>
    public void RestoreCachedSignalBoxElements(IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements)
    {
        if (elements.Count == 0)
        {
            return;
        }

        _cachedRemoteSignalBoxElements = elements;
        RefreshSignalBoxElements(elements);
    }

    private void ApplyBestAvailableSignalBoxElements()
    {
        var elements = _pendingSignalBoxElements is { Count: > 0 } pending
            ? pending
            : GetBestAvailableSignalBoxElements();
        _pendingSignalBoxElements = null;
        RefreshSignalBoxElements(elements);
    }

    private void RequestSignalBoxSnapshotRefreshIfEmpty()
    {
        if (SignalBoxElements.Count == 0)
        {
            RequestSignalBoxSnapshotRefresh();
        }
    }

    private bool HasAnySignalBoxElementsAvailable()
    {
        if (_cachedRemoteSignalBoxElements is { Count: > 0 })
        {
            return true;
        }

        return _mobaRuntime.Current.SignalBoxElements.Count > 0;
    }

    private void RequestSignalBoxSnapshotRefresh()
    {
        if (!IsMobaflowConnectionEnabled
            || !IsRestApiReachable
            || string.IsNullOrWhiteSpace(RestApiIpAddress)
            || RestApiPort <= 0
            || _runtimeHubRemoteClient == null)
        {
            return;
        }

        RunInBackground(RequestSignalBoxSnapshotRefreshAsync(), "Fetch runtime snapshot for SignalBox");
    }

    private async Task RequestSignalBoxSnapshotRefreshAsync()
    {
        try
        {
            await _runtimeHubRemoteClient!
                .RequestLatestSnapshotAsync(_applicationLifetimeCts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "On-demand runtime snapshot fetch failed");
        }
    }

    private void OnSolutionSyncedForSignalBox(SolutionSyncedEvent _)
    {
        _uiDispatcher.InvokeOnUi(() =>
        {
            if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true)
            {
                // Solution sync updates locomotive metadata only; signal aspects stay on MOBAflow.
                RequestSignalBoxSnapshotRefresh();
                return;
            }

            ApplyBestAvailableSignalBoxElements();
        });
    }
}
