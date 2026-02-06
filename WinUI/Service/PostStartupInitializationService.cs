// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using View;

/// <summary>
/// Defers non-critical initialization until after the main window is visible.
/// This improves perceived startup time by deferring heavy operations (plugins, REST discovery, health checks).
/// </summary>
public class PostStartupInitializationService
{
    private static readonly TimeSpan MinimumIndicatorDuration = TimeSpan.FromSeconds(1.5);
    private readonly HealthCheckService _healthCheckService;
    private readonly MainWindow _mainWindow;
    private readonly AppSettings _appSettings;
    private readonly ILogger<PostStartupInitializationService> _logger;

    public PostStartupInitializationService(
        HealthCheckService healthCheckService,
        MainWindow mainWindow,
        AppSettings appSettings,
        ILogger<PostStartupInitializationService> logger)
    {
        _healthCheckService = healthCheckService;
        _mainWindow = mainWindow;
        _appSettings = appSettings;
        _logger = logger;
    }

    /// <summary>
    /// Initializes deferred services asynchronously after the main window is visible.
    /// Non-blocking: errors are logged but do not crash the app.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        var indicatorTimer = Stopwatch.StartNew();
        var completionStatus = "Background services ready";

        _logger.LogInformation("[PostStartup] Starting deferred initialization");
        _logger.LogInformation("[PostStartup] Status indicator visible");
        _mainWindow.ViewModel.UpdatePostStartupInitializationStatus(true, "Initializing background services...");

        try
        {
            // Run tasks in parallel to minimize wall-clock time
            await Task.WhenAll(
                InitializeSpeechHealthCheckAsync(cancellationToken),
                InitializeWebAppAsync(cancellationToken)
                // Note: Plugin loading happens synchronously in ConfigureServices
                // as plugins may need to register ViewModels during startup
            );

            timer.Stop();
            _logger.LogInformation("[PostStartup] Deferred initialization completed in {ElapsedMs}ms", timer.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            completionStatus = "Startup canceled";
            _logger.LogInformation("[PostStartup] Deferred initialization cancelled");
        }
        catch (Exception ex)
        {
            completionStatus = "Startup failed";
            timer.Stop();
            _logger.LogError(ex, "[PostStartup] Deferred initialization failed after {ElapsedMs}ms", timer.ElapsedMilliseconds);
            // Do NOT rethrow - app should remain functional even if deferred services fail
        }
        finally
        {
            indicatorTimer.Stop();
            await EnsureMinimumIndicatorDurationAsync(indicatorTimer);
            _mainWindow.ViewModel.UpdatePostStartupInitializationStatus(false, completionStatus);
            _logger.LogInformation("[PostStartup] Status indicator hidden");
        }
    }

    private static async Task EnsureMinimumIndicatorDurationAsync(Stopwatch indicatorTimer)
    {
        var remaining = MinimumIndicatorDuration - indicatorTimer.Elapsed;
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining);
        }
    }

    /// <summary>
    /// Initializes and starts the health check service for speech engines.
    /// This can run in the background without blocking the UI.
    /// </summary>
    private async Task InitializeSpeechHealthCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("[PostStartup] Starting HealthCheckService");
            _mainWindow.ViewModel.UpdatePostStartupInitializationStatus(true, "Starting health checks...");
            _mainWindow.InitializeHealthChecks(_healthCheckService);
            _logger.LogInformation("[PostStartup] HealthCheckService started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostStartup] Failed to start HealthCheckService");
        }
    }

    /// <summary>
    /// Starts the ASP.NET Core WebApp for REST API if enabled.
    /// This can run in the background without blocking the UI.
    /// </summary>
    private Task InitializeWebAppAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_appSettings.Application?.AutoStartWebApp != true)
            {
                _logger.LogInformation("[PostStartup] WebApp disabled in settings");
                return Task.CompletedTask;
            }

            _logger.LogInformation("[PostStartup] Starting WebApp");
            // Note: App.StartWebAppIfEnabledAsync() handles the actual startup
            // We just log that we're in the deferred phase
            _logger.LogInformation("[PostStartup] WebApp startup delegated to App");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostStartup] Failed to start WebApp");
        }

        return Task.CompletedTask;
    }
}
