// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Moba.SharedUI.Interface;

#if ANDROID
using Android.Content;
using Microsoft.Maui.ApplicationModel;
#endif

/// <summary>
/// MAUI implementation of IBackgroundService for Android.
/// On other platforms, this is a no-op.
/// </summary>
public class BackgroundService : IBackgroundService
{
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public Task StartAsync(string title, string message)
    {
#if ANDROID
        var intent = new Intent(Platform.CurrentActivity, typeof(Platforms.Android.Services.Z21BackgroundService));
        intent.PutExtra("title", title);
        intent.PutExtra("message", message);

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
        {
            Platform.CurrentActivity?.StartForegroundService(intent);
        }
        else
        {
            Platform.CurrentActivity?.StartService(intent);
        }

        _isRunning = true;
#endif
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
#if ANDROID
        var intent = new Intent(Platform.CurrentActivity, typeof(Platforms.Android.Services.Z21BackgroundService));
        intent.SetAction("STOP_SERVICE");
        Platform.CurrentActivity?.StartService(intent);

        _isRunning = false;
#endif
        return Task.CompletedTask;
    }
}
