// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Platforms.Android;

using _Microsoft.Android.Resource.Designer;
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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Switch to MainTheme after splash screen (defined in Resources/values/styles.xml)
        SetTheme(Resource.Style.MainTheme);
    }

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
                // Fire-and-forget but with short timeout to not block Android
                var cleanupTask = Task.Run(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    try
                    {
                        await viewModel.DisconnectCommand.ExecuteAsync(null);
                        Debug.WriteLine("✅ MainActivity: Z21 cleanup complete");
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("⚠️ MainActivity: Cleanup timed out (2s)");
                    }
                });

                // Wait briefly for cleanup (Android may kill process otherwise)
                cleanupTask.Wait(TimeSpan.FromSeconds(2));
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ MainActivity: Cleanup error: {ex.Message}");
        }

        base.OnDestroy();
    }
}


