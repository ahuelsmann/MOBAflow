// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.



namespace Moba.MAUI.Service;



using SharedUI.Interface;

using SharedUI.ViewModel;



    /// <summary>
    /// Explicit startup hook for MOBAsmart singleton services that must be initialized at app launch.
    /// </summary>

public sealed class MobiStartupService

{

    private readonly RemoteRuntimeBridge _remoteRuntimeBridge;

    private readonly TrainControlViewModel _trainControlViewModel;

    private readonly ISolutionRemoteLoader? _solutionRemoteLoader;

    private readonly IMobileSolutionStore? _mobileSolutionStore;

    private readonly MauiViewModel _mauiViewModel;

    private readonly IUiDispatcher _uiDispatcher;



    public MobiStartupService(

        RemoteRuntimeBridge remoteRuntimeBridge,

        TrainControlViewModel trainControlViewModel,

        MauiViewModel mauiViewModel,

        IUiDispatcher uiDispatcher,

        ISolutionRemoteLoader? solutionRemoteLoader = null,

        IMobileSolutionStore? mobileSolutionStore = null)

    {

        _remoteRuntimeBridge = remoteRuntimeBridge;

        _trainControlViewModel = trainControlViewModel;

        _mauiViewModel = mauiViewModel;

        _uiDispatcher = uiDispatcher;

        _solutionRemoteLoader = solutionRemoteLoader;

        _mobileSolutionStore = mobileSolutionStore;

    }



    /// <summary>
    /// Ensures startup singletons are constructed and subscribed before the first page appears.
    /// </summary>

    public void Initialize()

    {

        _ = _remoteRuntimeBridge;

        _ = _trainControlViewModel;

        RunInBackground(RestoreCachedMobileDataAsync(), "Restore cached mobile solution");

    }



    private async Task RestoreCachedMobileDataAsync()

    {

        if (_mobileSolutionStore == null)

        {

            return;

        }



        var entry = await _mobileSolutionStore.TryLoadAsync().ConfigureAwait(false);

        if (entry == null)

        {

            return;

        }



        await _uiDispatcher.InvokeOnUiAsync(async () =>

        {

            if (_solutionRemoteLoader != null)

            {

                await _solutionRemoteLoader.TryLoadFromCacheAsync(entry).ConfigureAwait(true);

            }



            _mauiViewModel.RestoreCachedMobileSnapshot(entry);



            // SolutionSyncedEvent refreshes project locomotives; apply cached fleet metadata when present.

            if (entry.LocomotiveFleet.Count > 0)

            {

                _trainControlViewModel.RefreshLocomotiveList(entry.LocomotiveFleet);

            }

        }).ConfigureAwait(false);

    }



    private static void RunInBackground(Task task, string operationName)

    {

        _ = operationName;

        task.ContinueWith(

            static t => _ = t.Exception,

            TaskContinuationOptions.OnlyOnFaulted);

    }

}


