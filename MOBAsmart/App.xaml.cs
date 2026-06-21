// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Common.Extension;

using SharedUI.ViewModel;

using View;

public partial class App
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;

        RegisterCrashLogging();

        // Load App.xaml merged resource dictionaries first, then apply runtime theme colors.
        InitializeComponent();
        LoadThemeResources(isDark: true);
    }

    private static void RegisterCrashLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            WriteCrashLog(args.ExceptionObject?.ToString() ?? "UnhandledException without details.");
        };

#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
        {
            WriteCrashLog(args.Exception?.ToString() ?? "Android unhandled exception.");
            args.Handled = false;
        };
#endif
    }

    private static void WriteCrashLog(string message)
    {
        try
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "last-crash.txt");
            File.WriteAllText(path, $"{DateTimeOffset.Now:O}{Environment.NewLine}{message}");
        }
        catch
        {
            // Best-effort only.
        }
    }

    /// <summary>
    /// Applies the theme based on settings (manual or system theme).
    /// </summary>
    /// <param name="isDarkMode">Manual theme preference (true = Dark, false = Light)</param>
    /// <param name="useSystemTheme">Follow OS theme preference instead of manual</param>
    public void ApplyTheme(bool isDarkMode, bool useSystemTheme = false)
    {
        AppTheme targetTheme;
        bool effectiveIsDark;

        if (useSystemTheme)
        {
            // Follow OS theme
            targetTheme = AppTheme.Unspecified;
            effectiveIsDark = RequestedTheme == AppTheme.Dark;
        }
        else
        {
            // Manual theme control
            targetTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
            effectiveIsDark = isDarkMode;
        }

        UserAppTheme = targetTheme;
        LoadThemeResources(effectiveIsDark);

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

            resources["TabBarBackground"] = Color.FromArgb("#383838");
            resources["TabBarBorder"] = Color.FromArgb("#4D4D4D");
            resources["TabBarSelectedForeground"] = Color.FromArgb("#64B5F6");
            resources["TabBarUnselectedForeground"] = Color.FromArgb("#B0B0B0");
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

            resources["TabBarBackground"] = Color.FromArgb("#FFFFFF");
            resources["TabBarBorder"] = Color.FromArgb("#E0E0E0");
            resources["TabBarSelectedForeground"] = Color.FromArgb("#1976D2");
            resources["TabBarUnselectedForeground"] = Color.FromArgb("#757575");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Show SplashPage first, then navigate to the Shell root after settings are loaded.
        var splashPage = _services.GetRequiredService<SplashPage>();
        var window = new Window(splashPage);

        // ✅ Subscribe to lifecycle events for cleanup
        window.Destroying += OnWindowDestroying;

        return window;
    }

    /// <summary>
    /// Creates the main tab host after the splash screen.
    /// Kept intentionally light: CounterPage and other tabs load on first tab activation.
    /// </summary>
    public static Page CreateMainPage()
    {
        var services = ((App)Current!).Services;
        return services.GetRequiredService<AppTabHostPage>();
    }

    /// <summary>
    /// Creates the Shell root (optional wrapper around <see cref="AppTabHostPage"/>).
    /// </summary>
    public static Page CreateAppShell()
    {
        var services = ((App)Current!).Services;
        return services.GetRequiredService<AppShell>();
    }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider Services => _services;

    /// <inheritdoc />
    protected override void OnSleep()
    {
        base.OnSleep();
    }

    /// <inheritdoc />
    protected override void OnResume()
    {
        base.OnResume();

        var viewModel = _services.GetService<MauiViewModel>();
        if (viewModel != null)
        {
            viewModel.OnApplicationResumedAsync().Observe();
        }
    }

    /// <summary>
    /// Called when the window is being destroyed (app closing).
    /// Ensures Z21 disconnect and cleanup before app terminates.
    /// </summary>
    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        _ = e;
        CleanupOnWindowDestroyingAsync().Observe();
    }

    private async Task CleanupOnWindowDestroyingAsync()
    {
        try
        {
            // Get MauiViewModel and trigger graceful disconnect
            var viewModel = _services.GetService<MauiViewModel>();
            if (viewModel != null)
            {
                viewModel.NotifyApplicationStopping();
                if (viewModel.IsConnected)
                {
                    await viewModel.DisconnectCommand.ExecuteAsync(null);
                }
            }
        }
        catch (Exception)
        {
            // Ignore cleanup failures during application shutdown.
        }
    }
}