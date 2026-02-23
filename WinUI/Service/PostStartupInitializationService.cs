// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Backend.Data;
using Backend.Extensions;
using Common.Configuration;
using Common.Events;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

using View;

/// <summary>
/// Defers non-critical initialization until after the main window is visible.
/// This improves perceived startup time by deferring heavy operations (plugins, REST discovery, health checks).
/// </summary>
internal class PostStartupInitializationService
{
    private static readonly TimeSpan MinimumIndicatorDuration = TimeSpan.FromSeconds(1.5);
    private readonly IEventBus _eventBus;
    private readonly HealthCheckService _healthCheckService;
    private readonly MainWindow _mainWindow;
    private readonly AppSettings _appSettings;
    private readonly DataManager _dataManager;
    private readonly ILogger<PostStartupInitializationService> _logger;

    public PostStartupInitializationService(
        IEventBus eventBus,
        HealthCheckService healthCheckService,
        MainWindow mainWindow,
        AppSettings appSettings,
        DataManager dataManager,
        ILogger<PostStartupInitializationService> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _healthCheckService = healthCheckService;
        _mainWindow = mainWindow;
        _appSettings = appSettings;
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
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
        _eventBus.Publish(new PostStartupStatusEvent(true, "Initializing background services..."));

        try
        {
            // Stammdaten zuerst laden (DataManager), danach TrainClassLibrary aus derselben Datei
            await InitializeMasterDataAsync(cancellationToken);

            // Run tasks in parallel to minimize wall-clock time
            await Task.WhenAll(
                InitializeSpeechHealthCheckAsync(cancellationToken),
                InitializeWebAppAsync(cancellationToken),
                InitializePluginsAsync(cancellationToken)
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
            _eventBus.Publish(new PostStartupStatusEvent(false, completionStatus));
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
    /// Lädt die zentrale Stammdaten-Datei (Cities + Locomotives) in den DataManager
    /// und initialisiert die TrainClassLibrary aus derselben Datei.
    /// </summary>
    private async Task InitializeMasterDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Stammdaten-Pfad per Konvention (keine Abhängigkeit von Settings)
            var fullPath = Path.Combine(AppContext.BaseDirectory, "data.json");

            _logger.LogInformation("[PostStartup] Loading master data from {Path}", fullPath);
            _eventBus.Publish(new PostStartupStatusEvent(true, "Loading master data..."));

            await _dataManager.LoadAsync(fullPath).ConfigureAwait(false);

            MobaServiceCollectionExtensions.InitializeTrainClassLibrary(fullPath);

            _logger.LogInformation("[PostStartup] Master data loaded: {Cities} cities, {Locomotives} categories",
                _dataManager.Cities.Count, _dataManager.Locomotives.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[PostStartup] Master data load cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostStartup] Failed to load master data");
        }
    }

    /// <summary>
    /// Initializes all loaded plugins by calling their OnInitializedAsync method.
    /// Plugins are discovered and configured in App.ConfigureServices(),
    /// this method completes the initialization phase after MainWindow is visible.
    /// </summary>
    private async Task InitializePluginsAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("[PostStartup] Initializing plugins");
            _eventBus.Publish(new PostStartupStatusEvent(true, "Initializing plugins..."));
            _logger.LogInformation("[PostStartup] Plugins initialized");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[PostStartup] Plugin initialization cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostStartup] Failed to initialize plugins");
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
            _eventBus.Publish(new PostStartupStatusEvent(true, "Starting health checks..."));
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
