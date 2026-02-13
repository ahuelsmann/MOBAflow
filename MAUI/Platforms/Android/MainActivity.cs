// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Platforms.Android;

using global::Android.App;
using global::Android.Content.PM;
using global::Android.OS;
using global::Android.Views;
using SharedUI.ViewModel;
using Debug = System.Diagnostics.Debug;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // MAUI handles splash screen theme switching automatically via Maui.SplashTheme
    // No need to override OnCreate for theme switching

    /// <summary>
    /// Called when the activity is being destroyed (app closing, back button, swipe away).
    /// Ensures Z21 disconnect before Android terminates the process.
    /// CRITICAL: Without this, the Z21 keeps "zombie clients" that accumulate and can cause
    /// the Z21 to become unresponsive after many app restarts.
    /// </summary>
    protected override void OnDestroy()
    {
        Debug.WriteLine("🔄 MainActivity: OnDestroy - Starting Z21 cleanup...");

        try
        {
            // Get the MainWindowViewModel from DI and trigger cleanup
            var viewModel = IPlatformApplication.Current?.Services.GetService<MainWindowViewModel>();
            if (viewModel != null && viewModel.IsConnected)
            {
                // Async-first: start cleanup without synchronously blocking the Android lifecycle thread
                _ = CleanupAsync(viewModel);
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ MainActivity: Cleanup error: {ex.Message}");
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
                Debug.WriteLine("✅ MainActivity: Z21 cleanup complete");
            }
            else
            {
                Debug.WriteLine("⚠️ MainActivity: Cleanup timed out (2s)");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("⚠️ MainActivity: Cleanup canceled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ MainActivity: Cleanup error: {ex.Message}");
        }
    }
}


