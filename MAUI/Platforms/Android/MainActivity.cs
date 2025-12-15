// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Android.App;
using Android.Content.PM;
using Android.Views;

using Moba.SharedUI.ViewModel;

namespace Moba.Smart.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Switch to MainTheme after splash screen (defined in Resources/values/styles.xml)
        SetTheme(Microsoft.Maui.Resource.Style.MainTheme);
    }

    /// <summary>
    /// Called when the activity is being destroyed (app closing, back button, swipe away).
    /// Ensures Z21 disconnect before Android terminates the process.
    /// CRITICAL: Without this, the Z21 keeps "zombie clients" that accumulate and can cause
    /// the Z21 to become unresponsive after many app restarts.
    /// </summary>
    protected override void OnDestroy()
    {
        System.Diagnostics.Debug.WriteLine("🔄 MainActivity: OnDestroy - Starting Z21 cleanup...");

        try
        {
            // Get the CounterViewModel from DI and trigger cleanup
            var viewModel = IPlatformApplication.Current?.Services.GetService<CounterViewModel>();
            if (viewModel != null)
            {
                // Fire-and-forget but with short timeout to not block Android
                var cleanupTask = Task.Run(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    try
                    {
                        await viewModel.CleanupAsync();
                        System.Diagnostics.Debug.WriteLine("✅ MainActivity: Z21 cleanup complete");
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ MainActivity: Cleanup timed out (2s)");
                    }
                });

                // Wait briefly for cleanup (Android may kill process otherwise)
                cleanupTask.Wait(TimeSpan.FromSeconds(2));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ MainActivity: Cleanup error: {ex.Message}");
        }

        base.OnDestroy();
    }
}
