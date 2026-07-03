// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// MOBAsmart MOBAflow connection toggle and coordinator synchronization.
/// </summary>

public sealed partial class MauiViewModel

{

    private bool _mobaflowSessionWasActive;

    private bool _mobaflowInitialCatalogApplied;

    private bool _mobaflowConnectInProgress;

    private const int MobaflowConnectMaxAttempts = 3;

    private CancellationTokenSource? _mobaflowConnectCts;

    private readonly SemaphoreSlim _mobaflowCatalogSyncLock = new(1, 1);

    private bool HasStoredMobaflowEndpoint() =>
        !string.IsNullOrWhiteSpace(_settings.RestApi.CurrentIpAddress)
        && _settings.RestApi.Port > 0;

    /// <summary>
    /// Gets or sets whether MOBAsmart should discover and connect to MOBAflow.
    /// </summary>

    [ObservableProperty]

    private bool _isMobaflowConnectionEnabled;

    partial void OnIsMobaflowConnectionEnabledChanged(bool value)
    {
        _settings.RestApi.IsConnectionEnabled = value;
        NotifyMobaflowConnectionStatusProperties();
        UpdateRuntimeCoordinatorState();

        if (_isApplyingLoadedSettings)
        {
            return;
        }

        QueueSaveSettings();

        _mobaflowConnectCts?.Cancel();
        _mobaflowConnectCts?.Dispose();
        _mobaflowConnectCts = null;

        if (!value)
        {
            RunInBackground(DisconnectMobaflowAsync(), "Disconnect MOBAflow after toggle off");
            return;
        }

        _mobaflowConnectCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetimeCts.Token);
        RunInBackground(ApplyMobaflowConnectionStateAsync(_mobaflowConnectCts.Token), "Connect MOBAflow after toggle on");
    }

    [RelayCommand]
    private Task SetMobaflowConnectionAsync(bool enabled)
    {
        if (IsMobaflowConnectionEnabled != enabled)
        {
            IsMobaflowConnectionEnabled = enabled;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Connects using the saved endpoint without UDP discovery overwriting host/port.
    /// </summary>
    internal async Task<bool> ConnectToStoredEndpointAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _mobaflowConnectInProgress = true;

            cancellationToken.ThrowIfCancellationRequested();

            if (_runtimeHubRemoteClient?.IsConnected == true)
            {
                try
                {
                    await _runtimeHubRemoteClient.DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Runtime hub disconnect before reconnect failed");
                }
            }

            // Reconnect may repeat with unchanged endpoint; use the normal health-check timeout.
            var reachable = await RefreshRestApiReachableAsync(useConnectTimeout: false).ConfigureAwait(false);
            if (!reachable)
            {
                var anchor = string.IsNullOrWhiteSpace(Z21IpAddress) ? null : Z21IpAddress.Trim();
                await TryDiscoverMobaflowFastAsync(anchor, cancellationToken).ConfigureAwait(false);
                reachable = await RefreshRestApiReachableAsync(useConnectTimeout: false).ConfigureAwait(false);
            }

            if (!reachable)
            {
                return false;
            }

            return await EnsureRuntimeHubConnectionAsync(reachable, forceReconnect: true).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _mobaflowConnectInProgress = false;
        }
    }

    private async Task ApplyMobaflowConnectionStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_settings.RestApi.IsConnectionEnabled)
            {
                _mobaflowInitialCatalogApplied = false;
                await DisconnectMobaflowAsync().ConfigureAwait(false);
                return;
            }

            _mobaflowInitialCatalogApplied = false;
            _mobaflowConnectInProgress = true;

            cancellationToken.ThrowIfCancellationRequested();

            var anchor = string.IsNullOrWhiteSpace(Z21IpAddress) ? null : Z21IpAddress.Trim();

            // Discovery must finish before health-check; parallel probes often hit a stale saved IP first.
            await TryDiscoverMobaflowFastAsync(anchor, cancellationToken).ConfigureAwait(false);

            var reachable = false;
            for (var attempt = 0; attempt < MobaflowConnectMaxAttempts && !reachable; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (attempt > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken).ConfigureAwait(false);
                    await TryDiscoverMobaflowFastAsync(anchor, cancellationToken).ConfigureAwait(false);
                }

                reachable = await RefreshRestApiReachableAsync(useConnectTimeout: attempt == 0)
                    .ConfigureAwait(false);
            }

            if (!reachable)
            {
                await DiscoverMobaflowEndpointAsync(fullScan: true, anchor, cancellationToken).ConfigureAwait(false);
                reachable = await RefreshRestApiReachableAsync(useConnectTimeout: false).ConfigureAwait(false);
            }

            var hubConnected = !reachable
                || (_runtimeHubRemoteClient?.IsConnected ?? false)
                || await EnsureRuntimeHubConnectionAsync(true, forceReconnect: true).ConfigureAwait(false);

            if (!reachable)
            {
                _logger.LogDebug(
                    "MOBAflow not reachable after discovery; health loop will keep retrying (endpoint {Ip}:{Port})",
                    _settings.RestApi.CurrentIpAddress,
                    _settings.RestApi.Port);
            }
            else if (!hubConnected)
            {
                _logger.LogDebug(
                    "MOBAflow REST reachable at {Ip}:{Port} but RuntimeHub connect failed; health loop will retry",
                    _settings.RestApi.CurrentIpAddress,
                    _settings.RestApi.Port);
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer toggle or app shutdown.
        }
        finally
        {
            _mobaflowConnectInProgress = false;
        }
    }

    internal async Task MaybeDisableMobaflowConnectionWhenSessionLostAsync()
    {
        if (_mobaflowConnectInProgress || !_settings.RestApi.IsConnectionEnabled || IsRestApiReachable)
        {
            return;
        }

        await TryPeriodicRestDiscoveryIfNeededAsync().ConfigureAwait(false);
    }

    private async Task DisableMobaflowConnectionAfterFailedAttemptAsync()
    {
        await DisconnectMobaflowAsync().ConfigureAwait(false);

        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            if (IsMobaflowConnectionEnabled)
            {
                IsMobaflowConnectionEnabled = false;
            }

            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private void NotifyMobaflowConnectionStatusProperties()
    {
        OnPropertyChanged(nameof(RestApiStatusText));
        OnPropertyChanged(nameof(RestApiStatusSemanticDescription));
        OnPropertyChanged(nameof(RestApiIndicatorResourceKey));
    }

    /// <summary>
    /// Fast LAN discovery when the user enables MOBAflow (UDP + recent IPs; optional full /24 scan).
    /// </summary>
    private Task TryDiscoverMobaflowFastAsync(string? anchor, CancellationToken cancellationToken) =>
        DiscoverMobaflowEndpointAsync(fullScan: false, anchor, cancellationToken);

    /// <summary>
    /// Resolves the MOBApi endpoint on the LAN and persists it when found.
    /// </summary>
    private async Task DiscoverMobaflowEndpointAsync(
        bool fullScan,
        string? anchor,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _lastRestApiDiscoverTime = DateTime.UtcNow;

            var (ip, port) = await _restDiscoveryService
                .DiscoverServerFastAsync(anchor, cancellationToken)
                .ConfigureAwait(false);

            if ((string.IsNullOrEmpty(ip) || !port.HasValue) && fullScan)
            {
                (ip, port) = await _restDiscoveryService
                    .DiscoverServerAsync(anchor, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(ip) && port.HasValue)
            {
                await ApplyDiscoveredRestEndpointAsync(ip, port.Value, skipHealthCheck: false).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "MOBAflow LAN discovery failed (fullScan={FullScan})", fullScan);
        }
    }

    private async Task DisconnectMobaflowAsync()

    {

        if (_runtimeHubRemoteClient?.IsConnected == true)

        {

            try

            {

                await _runtimeHubRemoteClient.DisconnectAsync().ConfigureAwait(false);

            }

            catch (Exception ex)

            {

                _logger.LogDebug(ex, "Runtime hub disconnect failed during MOBAflow toggle off");

            }
        }

        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            IsRestApiReachable = false;
            SetRuntimeHubConnected(false);
            SetRemoteZ21Connected(false);
            UpdateRuntimeCoordinatorState();
            return Task.CompletedTask;
        }).ConfigureAwait(false);

    }

    /// <summary>
    /// Uses runtime snapshot connection state so coordinator routing stays accurate before UI properties catch up.
    /// </summary>
    private bool ResolveLocalZ21ConnectedForCoordinator() =>
        IsConnected || _mobaRuntime.Current.IsConnected;

    private void UpdateRuntimeCoordinatorState()
    {
        if (_mobileRuntimeCoordinator != null)

        {

            // Require an operational MOBAflow host (remote Z21), not just REST/SignalR reachability.
            var mobaflowSessionActive = IsMobaflowConnectionEnabled
                && IsRestApiReachable
                && IsRuntimeHubConnected
                && IsRemoteZ21Connected;

            var wasActive = _mobaflowSessionWasActive;

            var sessionEnded = wasActive && !mobaflowSessionActive;

            var sessionStarted = !wasActive && mobaflowSessionActive;

            _mobaflowSessionWasActive = mobaflowSessionActive;

            _mobileRuntimeCoordinator.SetMobaflowSessionActive(mobaflowSessionActive);

            _mobileRuntimeCoordinator.SetLocalZ21Connected(ResolveLocalZ21ConnectedForCoordinator());

            _eventBus.Publish(new RuntimeCommandAvailabilityChangedEvent());

            if (sessionEnded)

            {

                _mobaflowInitialCatalogApplied = false;

                RunInBackground(ActivateLocalProjectForZ21OnlyAsync(), "Activate cached project after MOBAflow session ended");

                ApplyBestAvailableSignalBoxElements();

                ApplyBestAvailableLocomotiveFleet();

            }

            if (sessionStarted)

            {

                ClearCachedRemoteLocomotiveFleet();

                RequestSignalBoxSnapshotRefresh();

                RunInBackground(ApplyInitialMobaflowCatalogAsync(), "Initial MOBAflow catalog import");

            }

            if (mobaflowSessionActive

                && !_mobaflowInitialCatalogApplied

                && (!HasAnySignalBoxElementsAvailable() || !HasAnyLocomotiveFleetAvailable()))

            {

                RequestSignalBoxSnapshotRefresh();

            }
        }

        RequestBackgroundServiceSync();

    }

    private async Task ActivateLocalProjectForZ21OnlyAsync()

    {

        if (_projectContext?.SelectedProject?.Model is not Project project)

        {

            return;

        }

        try

        {

            await _mobaRuntime

                .ActivateProjectAsync(project, _applicationLifetimeCts.Token)

                .ConfigureAwait(false);

        }

        catch (Exception ex)

        {

            _logger.LogDebug(ex, "Local project activation after MOBAflow session ended failed");

        }
    }

    private Task OnRuntimeHubSolutionUpdatedAsync(DateTimeOffset updatedAt)

    {

        _ = updatedAt;

        if (!IsMobaflowConnectionEnabled || _solutionRemoteLoader == null)

        {

            return Task.CompletedTask;

        }

        if (string.IsNullOrWhiteSpace(RestApiIpAddress) || RestApiPort <= 0)

        {

            return Task.CompletedTask;

        }

        if (_mobaflowInitialCatalogApplied)

        {

            return Task.CompletedTask;

        }

        RunInBackground(

            _solutionRemoteLoader.ForceSyncAsync(

                RestApiIpAddress,

                RestApiPort,

                _applicationLifetimeCts.Token),

            "Solution sync after MOBAflow push");

        return Task.CompletedTask;

    }

    private async Task SyncSolutionAfterHubConnectAsync()

    {

        if (!IsMobaflowConnectionEnabled || _solutionRemoteLoader == null)

        {

            return;

        }

        if (string.IsNullOrWhiteSpace(RestApiIpAddress) || RestApiPort <= 0)

        {

            return;

        }

        try

        {

            await _solutionRemoteLoader

                .ForceSyncAsync(RestApiIpAddress, RestApiPort, _applicationLifetimeCts.Token)

                .ConfigureAwait(false);

        }

        catch (Exception ex)

        {

            _logger.LogDebug(ex, "Initial solution sync after hub connect failed");

        }
    }

    /// <summary>
    /// One-time catalog import (signals + locomotives) after a MOBAflow session is established.
    /// Live snapshot streaming for aspects and locomotive state continues afterward.
    /// </summary>

    private async Task ApplyInitialMobaflowCatalogAsync()
    {
        if (!IsMobaflowConnectionEnabled || _mobaflowInitialCatalogApplied)
        {
            return;
        }

        await _mobaflowCatalogSyncLock.WaitAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
        try
        {
            if (!IsMobaflowConnectionEnabled || _mobaflowInitialCatalogApplied)
            {
                return;
            }

            await SyncSolutionAfterHubConnectAsync().ConfigureAwait(false);

            if (_runtimeHubRemoteClient != null)
            {
                try
                {
                    await _runtimeHubRemoteClient
                        .RequestLatestSnapshotAsync(_applicationLifetimeCts.Token)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Initial runtime snapshot request during catalog import failed");
                }
            }

            _uiDispatcher.InvokeOnUiLowPriority(() =>
            {
                ApplyBestAvailableSignalBoxElements();
                ApplyBestAvailableLocomotiveFleet();
                _mobaflowInitialCatalogApplied = true;
            });
        }
        finally
        {
            _mobaflowCatalogSyncLock.Release();
        }
    }

    /// <summary>
    /// Re-fetches the MOBAflow solution for train control when the Control tab becomes active.
    /// </summary>

    public async Task RequestSolutionSyncAsync(CancellationToken cancellationToken = default)

    {

        if ((_projectContext?.SelectedProject?.Locomotives.Count ?? 0) == 0

            && _solutionRemoteLoader != null)

        {

            try

            {

                await _solutionRemoteLoader

                    .TryLoadFromCacheAsync(cancellationToken: cancellationToken)

                    .ConfigureAwait(false);

            }

            catch (Exception ex)

            {

                _logger.LogDebug(ex, "On-demand cached solution load failed");

            }
        }

        if (!IsMobaflowConnectionEnabled

            || _solutionRemoteLoader == null

            || string.IsNullOrWhiteSpace(RestApiIpAddress)

            || RestApiPort <= 0)

        {

            return;

        }

        if (!_mobaflowInitialCatalogApplied)

        {

            try

            {

                await _solutionRemoteLoader

                    .ForceSyncAsync(RestApiIpAddress, RestApiPort, cancellationToken)

                    .ConfigureAwait(false);

            }

            catch (Exception ex)

            {

                _logger.LogDebug(ex, "On-demand solution sync failed");

            }
        }

        _uiDispatcher.InvokeOnUi(() =>

        {

            ApplyBestAvailableLocomotiveFleet();

            if (!HasAnyLocomotiveFleetAvailable())

            {

                RequestSignalBoxSnapshotRefresh();

            }
        });

    }
}

