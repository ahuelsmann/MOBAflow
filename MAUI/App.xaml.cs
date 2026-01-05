// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Common.Configuration;
using SharedUI.Interface;
using SharedUI.ViewModel;
using System.Diagnostics;

public partial class App
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        
        // ⚠️ CRITICAL: Load default dark theme resources BEFORE InitializeComponent
        // Theme will be properly applied after settings are loaded in CreateWindow
        LoadThemeResources(isDark: true);
        
        InitializeComponent();
    }

    /// <summary>
    /// Applies the theme based on the saved preference or system default.
    /// </summary>
    public void ApplyTheme(string themePreference)
    {
        AppTheme targetTheme;
        
        switch (themePreference)
        {
            case "Light":
                targetTheme = AppTheme.Light;
                break;
            case "Dark":
                targetTheme = AppTheme.Dark;
                break;
            default: // "System" or unknown
                targetTheme = AppTheme.Unspecified;
                break;
        }
        
        UserAppTheme = targetTheme;
        
        // Apply theme-specific resources
        var isDark = targetTheme == AppTheme.Dark || 
                     (targetTheme == AppTheme.Unspecified && RequestedTheme == AppTheme.Dark);
        
        LoadThemeResources(isDark);
        
        Debug.WriteLine($"🎨 Theme applied: {themePreference} (isDark: {isDark})");
    }

    /// <summary>
    /// Loads theme-specific color resources dynamically.
    /// </summary>
    private void LoadThemeResources(bool isDark)
    {
        var resources = Resources;
        
        if (isDark)
        {
            // Dark theme colors
            resources["SurfaceBackground"] = Color.FromArgb("#121212");
            resources["SurfaceCard"] = Color.FromArgb("#2D2D30");
            resources["SurfaceElevated"] = Color.FromArgb("#383838");
            resources["SurfaceHighlight"] = Color.FromArgb("#404040");
            resources["SurfaceDark"] = Color.FromArgb("#1E1E1E");
            resources["SurfaceVariant"] = Color.FromArgb("#2C2C2C");
            resources["Surface"] = Color.FromArgb("#1E1E1E");
            
            resources["RailwayPrimary"] = Color.FromArgb("#64B5F6");
            resources["RailwaySecondary"] = Color.FromArgb("#FF9800");
            resources["RailwayAccent"] = Color.FromArgb("#81C784");
            resources["RailwayDanger"] = Color.FromArgb("#EF5350");
            resources["RailwayWarning"] = Color.FromArgb("#FFB74D");
            
            resources["TextPrimary"] = Color.FromArgb("#FFFFFF");
            resources["TextSecondary"] = Color.FromArgb("#B0B0B0");
            resources["TextDisabled"] = Color.FromArgb("#606060");
            resources["TextOnPrimary"] = Color.FromArgb("#000000");
            resources["BorderColor"] = Color.FromArgb("#4D4D4D");
            
            resources["PageBackgroundColor"] = Color.FromArgb("#121212");
            resources["FrameBackgroundColor"] = Color.FromArgb("#1E1E1E");
            resources["Primary"] = Color.FromArgb("#64B5F6");
            resources["White"] = Colors.White;
            resources["Gray200"] = Color.FromArgb("#3C3C3C");
            resources["Gray300"] = Color.FromArgb("#4A4A4A");
            resources["Gray400"] = Color.FromArgb("#606060");
            resources["Gray600"] = Color.FromArgb("#404040");
        }
        else
        {
            // Light theme colors
            resources["SurfaceBackground"] = Color.FromArgb("#FAFAFA");
            resources["SurfaceCard"] = Color.FromArgb("#FFFFFF");
            resources["SurfaceElevated"] = Color.FromArgb("#FFFFFF");
            resources["SurfaceHighlight"] = Color.FromArgb("#E0E0E0");
            resources["SurfaceDark"] = Color.FromArgb("#F5F5F5");
            resources["SurfaceVariant"] = Color.FromArgb("#EEEEEE");
            resources["Surface"] = Color.FromArgb("#FFFFFF");
            
            resources["RailwayPrimary"] = Color.FromArgb("#1976D2");
            resources["RailwaySecondary"] = Color.FromArgb("#FF6F00");
            resources["RailwayAccent"] = Color.FromArgb("#4CAF50");
            resources["RailwayDanger"] = Color.FromArgb("#D32F2F");
            resources["RailwayWarning"] = Color.FromArgb("#FFA000");
            
            resources["TextPrimary"] = Color.FromArgb("#212121");
            resources["TextSecondary"] = Color.FromArgb("#757575");
            resources["TextDisabled"] = Color.FromArgb("#BDBDBD");
            resources["TextOnPrimary"] = Color.FromArgb("#FFFFFF");
            resources["BorderColor"] = Color.FromArgb("#E0E0E0");
            
            resources["PageBackgroundColor"] = Color.FromArgb("#FAFAFA");
            resources["FrameBackgroundColor"] = Color.FromArgb("#FFFFFF");
            resources["Primary"] = Color.FromArgb("#1976D2");
            resources["White"] = Colors.White;
            resources["Gray200"] = Color.FromArgb("#EEEEEE");
            resources["Gray300"] = Color.FromArgb("#E0E0E0");
            resources["Gray400"] = Color.FromArgb("#BDBDBD");
            resources["Gray600"] = Color.FromArgb("#F5F5F5");
        }
        
        Resources = resources;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // ✅ CRITICAL: Load settings BEFORE creating MainPage
        Debug.WriteLine("🚀 App.CreateWindow: Loading settings...");
        var settingsService = _services.GetRequiredService<ISettingsService>();
        
        // ⚠️ BLOCKING: Wait for settings to load (on background thread to avoid UI freeze)
        Task.Run(async () => await settingsService.LoadSettingsAsync()).Wait();
        
        // ✅ Apply saved theme preference (defaults to "System" if not set)
        var settings = settingsService.GetSettings();
        ApplyTheme(settings.Application.Theme);
        Debug.WriteLine($"✅ App.CreateWindow: Theme '{settings.Application.Theme}' applied");

        Debug.WriteLine("✅ App.CreateWindow: Settings loaded, creating MainPage...");

        // ✅ Create MainPage AFTER settings are loaded
        var mainPage = _services.GetRequiredService<MainPage>();
        var window = new Window(mainPage);

        // ✅ Subscribe to lifecycle events for cleanup
        window.Destroying += OnWindowDestroying;

        Debug.WriteLine("✅ App.CreateWindow: Window created successfully");
        return window;
    }

    /// <summary>
    /// Called when the window is being destroyed (app closing).
    /// Ensures Z21 disconnect and cleanup before app terminates.
    /// </summary>
    private async void OnWindowDestroying(object? sender, EventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        
        try
        {
            Debug.WriteLine("🔄 App: OnWindowDestroying - Starting cleanup...");

            // Get MauiViewModel and trigger graceful disconnect
            var viewModel = _services.GetService<MauiViewModel>();
            if (viewModel != null)
            {
                if (viewModel.IsConnected)
                {
                    await viewModel.DisconnectCommand.ExecuteAsync(null);
                }
                Debug.WriteLine("✅ App: MauiViewModel cleanup complete");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ App: Cleanup error: {ex.Message}");
        }
    }
}
