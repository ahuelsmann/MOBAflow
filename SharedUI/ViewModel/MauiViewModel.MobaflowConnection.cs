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

    private CancellationTokenSource? _mobaflowConnectCts;

    private readonly SemaphoreSlim _mobaflowCatalogSyncLock = new(1, 1);



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

    /// <summary>
    /// Retries MOBAflow discovery and hub connection while the toggle remains enabled.
    /// </summary>
    public Task RetryMobaflowConnectionAsync()
    {
        if (!IsMobaflowConnectionEnabled)
        {
            return Task.CompletedTask;
        }

        _mobaflowConnectCts?.Cancel();
        _mobaflowConnectCts?.Dispose();
        _mobaflowConnectCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetimeCts.Token);
        return ApplyMobaflowConnectionStateAsync(_mobaflowConnectCts.Token);
    }

    [RelayCommand]
    private Task SetMobaflowConnectionAsync(bool enabled)
    {
        if (IsMobaflowConnectionEnabled != enabled)
        {
            IsMobaflowConnectionEnabled = enabled;
            return Task.CompletedTask;
        }

        return enabled ? RetryMobaflowConnectionAsync() : Task.CompletedTask;
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

            foreach (var delayMs in MobaflowConnectAttemptDelaysMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }

                if (!_settings.RestApi.IsConnectionEnabled)
                {
                    return;
                }

                var reachable = await RefreshRestApiReachableAsync().ConfigureAwait(false);
                if (!reachable)
                {
                    await TryDiscoverMobaflowEndpointsAsync().ConfigureAwait(false);
                    reachable = await RefreshRestApiReachableAsync().ConfigureAwait(false);
                }

                if (reachable && (_runtimeHubRemoteClient?.IsConnected ?? false))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer toggle or app shutdown.
        }
    }

    private void NotifyMobaflowConnectionStatusProperties()
    {
        OnPropertyChanged(nameof(RestApiStatusText));
        OnPropertyChanged(nameof(RestApiStatusSemanticDescription));
        OnPropertyChanged(nameof(RestApiIndicatorResourceKey));
    }



    /// <summary>

    /// Discovers MOBApi on the LAN and applies the REST endpoint. Does not trigger Z21 discovery.

    /// </summary>

    private async Task TryDiscoverMobaflowEndpointsAsync()

    {

        try

        {

            await Task.Delay(TimeSpan.FromMilliseconds(500), _applicationLifetimeCts.Token).ConfigureAwait(false);



            var (fastIp, fastPort) = await _restDiscoveryService

                .DiscoverServerFastAsync(string.IsNullOrWhiteSpace(Z21IpAddress) ? null : Z21IpAddress.Trim())

                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(fastIp) && fastPort.HasValue)

            {

                await ApplyDiscoveredRestEndpointAsync(fastIp, fastPort.Value).ConfigureAwait(false);

                return;

            }



            await RestDiscoveryLoopAsync().ConfigureAwait(false);

        }

        catch (OperationCanceledException)

        {

            // App shutdown.

        }

        catch (Exception ex)

        {

            _logger.LogDebug(ex, "MOBAflow-only discovery failed");

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



    private void UpdateRuntimeCoordinatorState()

    {

        if (_mobileRuntimeCoordinator != null)

        {

            var mobaflowSessionActive = IsMobaflowConnectionEnabled

                && IsRestApiReachable

                && IsRuntimeHubConnected;

            var wasActive = _mobaflowSessionWasActive;

            var sessionEnded = wasActive && !mobaflowSessionActive;

            var sessionStarted = !wasActive && mobaflowSessionActive;

            _mobaflowSessionWasActive = mobaflowSessionActive;



            _mobileRuntimeCoordinator.SetMobaflowSessionActive(mobaflowSessionActive);

            _mobileRuntimeCoordinator.SetLocalZ21Connected(IsConnected);

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


