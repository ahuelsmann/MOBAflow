// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Mobile;

using Interface;

using TrainControl;

/// <summary>
/// Explicit startup hook for MOBAsmart singleton services that must be initialized at app launch.
/// </summary>
public sealed class MobiStartupService(
    RemoteRuntimeBridge remoteRuntimeBridge,
    TrainControlViewModel trainControlViewModel,
    MauiViewModel mauiViewModel,
    IUiDispatcher uiDispatcher,
    ISolutionRemoteLoader solutionRemoteLoader,
    IMobileSolutionStore mobileSolutionStore)
{
    private readonly RemoteRuntimeBridge _remoteRuntimeBridge = remoteRuntimeBridge;
    private readonly TrainControlViewModel _trainControlViewModel = trainControlViewModel;
    private readonly ISolutionRemoteLoader _solutionRemoteLoader = solutionRemoteLoader;
    private readonly IMobileSolutionStore _mobileSolutionStore = mobileSolutionStore;
    private readonly MauiViewModel _mauiViewModel = mauiViewModel;
    private readonly IUiDispatcher _uiDispatcher = uiDispatcher;

    /// <summary>
    /// Ensures startup singletons are constructed and subscribed before the first page appears.
    /// </summary>
    public void Initialize()
    {
        _ = _remoteRuntimeBridge;
        _ = _trainControlViewModel;
        RunInBackground(RestoreCachedMobileDataAsync());
    }

    private async Task RestoreCachedMobileDataAsync()
    {
        var entry = await _mobileSolutionStore.TryLoadAsync().ConfigureAwait(false);
        if (entry == null)
        {
            return;
        }

        await _uiDispatcher.InvokeOnUiAsync(async () =>
        {
            // Restore cached runtime metadata before solution sync so event handlers see the cache.
            _mauiViewModel.RestoreCachedMobileSnapshot(entry);
            await _solutionRemoteLoader.TryLoadFromCacheAsync(entry).ConfigureAwait(true);
            _mauiViewModel.ApplyRestoredMobileCacheToUi();
            _trainControlViewModel.RefreshLocomotiveList(_mauiViewModel.GetStartupLocomotiveFleet());
        }).ConfigureAwait(false);
    }

    private static void RunInBackground(Task task)
    {
        task.ContinueWith(
            static t => _ = t.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
