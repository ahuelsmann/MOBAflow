// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Runtime;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Android foreground-service keep-alive for active Z21 and MOBAflow connections.
/// </summary>
public sealed partial class MauiViewModel
{
    private readonly SemaphoreSlim _backgroundServiceSyncLock = new(1, 1);
    private bool _shouldReconnectLocalZ21OnResume;

    private bool ShouldKeepAliveInBackground() =>
        MobileBackgroundKeepAlivePolicy.ShouldKeepAlive(
            IsConnected,
            IsMobaflowConnectionEnabled,
            IsRestApiReachable,
            IsRuntimeHubConnected);

    private bool IsMobaflowSessionActive() =>
        IsMobaflowConnectionEnabled && IsRestApiReachable && IsRuntimeHubConnected;

    private void RequestBackgroundServiceSync() =>
        RunInBackground(SyncBackgroundServiceAsync(), "Sync foreground background service");

    private async Task SyncBackgroundServiceAsync()
    {
        if (_backgroundService == null || _isStopping)
        {
            return;
        }

        await _backgroundServiceSyncLock.WaitAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
        try
        {
            if (!ShouldKeepAliveInBackground())
            {
                if (_backgroundService.IsRunning)
                {
                    await _backgroundService.StopAsync().ConfigureAwait(false);
                }

                return;
            }

            var message = MobileBackgroundKeepAlivePolicy.GetNotificationMessage(
                IsConnected,
                IsMobaflowSessionActive());
            await _backgroundService.StartAsync("MOBAsmart Active", message).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during application shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Foreground background service sync failed");
        }
        finally
        {
            _backgroundServiceSyncLock.Release();
        }
    }

    private async Task StopBackgroundServiceAsync()
    {
        if (_backgroundService == null || !_backgroundService.IsRunning)
        {
            return;
        }

        try
        {
            await _backgroundService.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Foreground background service stop failed");
        }
    }

    /// <summary>
    /// Refreshes remote connections and the foreground service after the app returns from background.
    /// </summary>
    public async Task OnApplicationResumedAsync()
    {
        if (_isStopping)
        {
            return;
        }

        try
        {
            await RefreshRestApiReachableAsync().ConfigureAwait(false);

            if (_shouldReconnectLocalZ21OnResume
                && !IsConnected
                && !string.IsNullOrWhiteSpace(Z21IpAddress)
                && !_mobaRuntime.Current.IsConnected)
            {
                await ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
            }

            await SyncBackgroundServiceAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Application resume handling failed");
        }
    }
}
