// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Platforms.Android;

using global::Android.App;
using global::Android.Content.PM;
using global::Android.OS;
using global::Android.Views;

using Microsoft.Extensions.DependencyInjection;

using Common.Extension;

using SharedUI.ViewModel;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    /// <summary>
    /// Creates the Android activity without restoring a stale fragment hierarchy.
    /// </summary>
    /// <param name="savedInstanceState">The previously saved activity state.</param>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        _ = savedInstanceState;
        base.OnCreate(null);
    }

    /// <summary>
    /// Called when the activity is being destroyed (app closing, back button, swipe away).
    /// Ensures Z21 disconnect before Android terminates the process.
    /// CRITICAL: Without this, the Z21 keeps "zombie clients" that accumulate and can cause
    /// the Z21 to become unresponsive after many app restarts.
    /// </summary>
    protected override void OnDestroy()
    {
        var services = IPlatformApplication.Current?.Services;

        try
        {
            // Get the MainWindowViewModel from DI and trigger cleanup
            var viewModel = services?.GetService<MainWindowViewModel>();
            if (viewModel != null && viewModel.IsConnected)
            {
                // Async-first: start cleanup without synchronously blocking the Android lifecycle thread
                CleanupAsync(viewModel).Observe();
            }

        }
        catch (Exception)
        {
            // Ignore cleanup failures during activity shutdown.
        }

        base.OnDestroy();
    }

    private static async Task CleanupAsync(MainWindowViewModel viewModel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            var disconnectTask = viewModel.DisconnectCommand.ExecuteAsync(null);
            var completed = await Task.WhenAny(disconnectTask, Task.Delay(Timeout.Infinite, cts.Token)).ConfigureAwait(false);

            if (completed == disconnectTask)
            {
                await disconnectTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation during shutdown.
        }
        catch (Exception)
        {
            // Ignore cleanup failures during shutdown.
        }
    }

}


