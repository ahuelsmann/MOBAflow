// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Microsoft.Extensions.Logging;

using Mobile;

/// <summary>
/// Android foreground-service keep-alive for active Z21 and MOBAflow connections.
/// </summary>
public sealed partial class MauiViewModel
{
    private bool _shouldReconnectLocalZ21OnResume;

    private MobileKeepAliveState GetKeepAliveState() =>
        new(IsConnected, IsMobaflowConnectionEnabled, IsRestApiReachable, IsRuntimeHubConnected);

    private void RequestBackgroundServiceSync() =>
        RunInBackground(SyncBackgroundServiceAsync(), "Sync foreground background service");

    private Task SyncBackgroundServiceAsync() =>
        _appLifecycle.SyncBackgroundServiceAsync(GetKeepAliveState, _applicationLifetimeCts.Token);

    /// <summary>
    /// Refreshes remote connections and the foreground service after the app returns from background.
    /// </summary>
    public async Task OnApplicationResumedAsync()
    {
        if (_appLifecycle.IsStopping)
        {
            return;
        }

        try
        {
            await RefreshRestApiReachableAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);

            if (_shouldReconnectLocalZ21OnResume
                && !IsConnected
                && !string.IsNullOrWhiteSpace(Z21IpAddress)
                && !_runtimeSnapshots.Current.IsConnected)
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
