// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;

using Serilog;

using Service;

using System.Diagnostics;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    private Window? _window;
    private IServiceProvider? _services;
    private ILogger<App> _logger = NullLogger<App>.Instance;

    /// <summary>
    /// Gets the main application window (for folder/file pickers and similar).
    /// </summary>
    public static Window? MainWindow => Current._window;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    ///
    /// PERFORMANCE NOTE: Kept minimal - DI and heavy initialization run in OnLaunched after the
    /// startup splash is dismissed. PostStartupInitializationService runs after MainWindow is visible.
    /// </summary>
    public App()
    {
        try
        {
            Debug.WriteLine("[Startup] App constructor started");
            InitializeComponent();
            Debug.WriteLine("[Startup] App XAML initialized");

            UnhandledException += OnUnhandledException;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Startup] FATAL ERROR during App initialization: {ex}");
            throw;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var message = $"UNHANDLED EXCEPTION: {e.Exception.GetType().Name}: {e.Exception.Message}";
        _logger.LogCritical(e.Exception, "Unhandled exception in WinUI application: {Message}", message);
        e.Handled = false;
    }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("Services are not available before OnLaunched completes service configuration.");

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var startupService = new WinUiAppStartupService(this);

        try
        {
            var result = startupService.Launch(args);
            _services = result.Services;
            _window = result.MainWindow;
            _logger = result.Logger;
        }
        catch (Exception ex)
        {
            startupService.AbortLaunch();

            if (_logger is NullLogger<App>)
            {
                Log.Fatal(ex, "OnLaunched failed before logging services were available");
            }
            else
            {
                _logger.LogCritical(ex, "OnLaunched failed");
            }

            throw;
        }
    }
}
