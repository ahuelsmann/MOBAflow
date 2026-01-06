// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Timer = System.Timers.Timer;

namespace Moba.WinUI.Service;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sound;

/// <summary>
/// Centralized health check service that monitors Azure Speech Service and other dependencies.
/// Provides periodic health checks and status reporting for UI display.
/// </summary>
public class HealthCheckService : IDisposable
{
    private readonly SpeechHealthCheck _speechHealthCheck;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IConfiguration _configuration;
    private Timer? _healthCheckTimer;
    private bool _disposed;

    public HealthCheckService(
        SpeechHealthCheck speechHealthCheck,
        ILogger<HealthCheckService> logger,
        IConfiguration configuration)
    {
        _speechHealthCheck = speechHealthCheck;
        _logger = logger;
        _configuration = configuration;

        // Initialize status
        SpeechServiceStatus = "⏳ Initializing...";
    }

    /// <summary>
    /// Current status of Azure Speech Service.
    /// Can be bound to UI elements to display health status.
    /// </summary>
    public string SpeechServiceStatus { get; private set; }

    /// <summary>
    /// Indicates whether Azure Speech Service is healthy.
    /// </summary>
    public bool IsSpeechServiceHealthy { get; private set; }

    /// <summary>
    /// Event raised when health status changes.
    /// </summary>
    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    /// <summary>
    /// Starts periodic health checks based on configuration.
    /// </summary>
    public void StartPeriodicChecks()
    {
        // Perform initial check immediately (even if periodic checks are disabled)
        _ = PerformHealthCheckAsync();

        var enabled = _configuration.GetValue("HealthCheck:Enabled", true);
        if (!enabled)
        {
            Console.WriteLine("ℹ️ Periodic health checks disabled in configuration");
            _logger.LogInformation("Periodic health checks disabled in configuration (initial check performed)");
            return;
        }

        var intervalSeconds = _configuration.GetValue("HealthCheck:IntervalSeconds", 60);
        Console.WriteLine($"🔄 Starting periodic health checks every {intervalSeconds} seconds");
        _logger.LogInformation("Starting periodic health checks every {Interval} seconds", intervalSeconds);

        _healthCheckTimer = new Timer(intervalSeconds * 1000);
        _healthCheckTimer.Elapsed += async (_, _) => await PerformHealthCheckAsync();
        _healthCheckTimer.AutoReset = true;
        _healthCheckTimer.Start();
    }

    /// <summary>
    /// Performs a health check of all monitored services.
    /// </summary>
    public async Task PerformHealthCheckAsync()
    {
        Console.WriteLine("🔍 Performing health check...");
        _logger.LogDebug("Performing health check...");

        try
        {
            // Check Azure Speech Service
            var isConfigured = _speechHealthCheck.IsConfigured();
            var isHealthy = isConfigured && await _speechHealthCheck.TestConnectivityAsync();

            var previousStatus = SpeechServiceStatus;
            var previousHealthy = IsSpeechServiceHealthy;

            if (!isConfigured)
            {
                SpeechServiceStatus = "⚠️ Not Configured";
                IsSpeechServiceHealthy = false;
                Console.WriteLine("⚠️ Azure Speech Service: Not Configured");
            }
            else if (isHealthy)
            {
                SpeechServiceStatus = "✅ Ready";
                IsSpeechServiceHealthy = true;
                Console.WriteLine("✅ Azure Speech Service: Ready");
            }
            else
            {
                SpeechServiceStatus = "❌ Connection Failed";
                IsSpeechServiceHealthy = false;
                Console.WriteLine("❌ Azure Speech Service: Connection Failed");
            }

            // Notify if status changed
            if (SpeechServiceStatus != previousStatus || IsSpeechServiceHealthy != previousHealthy)
            {
                Console.WriteLine($"📊 Health status changed: {SpeechServiceStatus}");
                _logger.LogInformation("Health status changed: {Status}", SpeechServiceStatus);
                OnHealthStatusChanged(new HealthStatusChangedEventArgs
                {
                    ServiceName = "AzureSpeech",
                    IsHealthy = IsSpeechServiceHealthy,
                    StatusMessage = SpeechServiceStatus
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Health check failed with exception: {ex.Message}");
            _logger.LogError(ex, "Health check failed with exception");
            SpeechServiceStatus = "❌ Check Failed";
            IsSpeechServiceHealthy = false;
        }
    }

    /// <summary>
    /// Stops periodic health checks.
    /// </summary>
    public void StopPeriodicChecks()
    {
        _healthCheckTimer?.Stop();
        Console.WriteLine("⏸️ Periodic health checks stopped");
        _logger.LogInformation("Periodic health checks stopped");
    }

    protected virtual void OnHealthStatusChanged(HealthStatusChangedEventArgs e)
    {
        HealthStatusChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Dispose managed resources
            StopPeriodicChecks();
            _healthCheckTimer?.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// Event args for health status changes.
/// </summary>
public class HealthStatusChangedEventArgs : EventArgs
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}