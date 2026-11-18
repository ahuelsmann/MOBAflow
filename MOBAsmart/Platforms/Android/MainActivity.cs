// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Platforms.Android;

using global::Android.App;
using global::Android.Content.PM;
using global::Android.OS;
using global::Android.Views;

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
}
