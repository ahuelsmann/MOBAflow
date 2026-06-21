// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Platforms.Android.Services;

using SharedUI.Interface;

#if ANDROID
using global::Android.Content;
using global::Android.OS;

using Microsoft.Maui.ApplicationModel;
#endif

/// <summary>
/// MAUI implementation of <see cref="IBackgroundService"/> for Android.
/// On other platforms, this is a no-op.
/// </summary>
public sealed class BackgroundService : IBackgroundService
{
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public async Task StartAsync(string title, string message)
    {
#if ANDROID
        await MainThread.InvokeOnMainThreadAsync(MobiAndroidPermissions.EnsureForegroundServicePermissionsAsync)
            .ConfigureAwait(false);

        var context = Platform.CurrentActivity ?? Platform.AppContext;
        var intent = new Intent(context, typeof(Z21BackgroundService));
        intent.PutExtra("title", title);
        intent.PutExtra("message", message);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }

        _isRunning = true;
#else
        await Task.CompletedTask.ConfigureAwait(false);
#endif
    }

    public Task StopAsync()
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Platform.AppContext;
        var intent = new Intent(context, typeof(Z21BackgroundService));
        intent.SetAction("STOP_SERVICE");
        context.StartService(intent);

        _isRunning = false;
#endif
        return Task.CompletedTask;
    }
}
