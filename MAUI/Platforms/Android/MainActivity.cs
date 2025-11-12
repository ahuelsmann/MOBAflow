using Android.App;
using Android.Content.PM;
using Android.Views;

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
        
        // Switch to MainTheme after splash screen
        SetTheme(Resource.Style.MainTheme);
    }
}
