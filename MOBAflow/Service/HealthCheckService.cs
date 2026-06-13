// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Timer = System.Timers.Timer;

namespace Moba.WinUI.Service;

using Common.Extension;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Sound;

/// <summary>
/// Centralized health check service that monitors the selected speech service and provides
/// periodic status updates for UI display.
/// </summary>
public partial class HealthCheckService : IDisposable
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
    /// Current status of the speech service.
    /// Can be bound to UI elements to display health status.
    /// </summary>
    public string SpeechServiceStatus { get; private set; }

    /// <summary>
    /// Indicates whether the speech service is healthy.
    /// </summary>
    public bool IsSpeechServiceHealthy { get; private set; }

    /// <summary>
    /// Event raised when health status changes.
    /// </summary>
    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    /// <summary>
    /// Starts periodic health checks based on configuration.
    /// 
    /// NOTE: Initial health check is executed asynchronously without await.
    /// This is intentional - we don't want to block service initialization.
    /// If the check fails, it's logged but doesn't prevent app startup.
    /// </summary>
    public void StartPeriodicChecks()
    {
        // Perform initial check immediately (even if periodic checks are disabled).
        // Fire-and-forget: we don't want to block service initialization.
        // Periodic checks (if enabled) will retry soon anyway.
        PerformHealthCheckAsync()
            .SafeFireAndForget(ex => _logger.LogError(ex, "Initial health check failed"));

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
        _healthCheckTimer.Elapsed += (_, _) =>
            PerformHealthCheckAsync().SafeFireAndForget(ex => _logger.LogError(ex, "Periodic health check failed"));
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
            // Check local Piper TTS configuration.
            var isConfigured = _speechHealthCheck.IsConfigured();
            var isHealthy = isConfigured && await _speechHealthCheck.TestConnectivityAsync();

            var previousStatus = SpeechServiceStatus;
            var previousHealthy = IsSpeechServiceHealthy;

            if (!isConfigured)
            {
                SpeechServiceStatus = "⚠️ Not Configured";
                IsSpeechServiceHealthy = false;
                Console.WriteLine("⚠️ Piper TTS: Not Configured");
            }
            else if (isHealthy)
            {
                SpeechServiceStatus = "✅ Ready";
                IsSpeechServiceHealthy = true;
                Console.WriteLine("✅ Piper TTS: Ready");
            }
            else
            {
                SpeechServiceStatus = "❌ Connection Failed";
                IsSpeechServiceHealthy = false;
                Console.WriteLine("❌ Piper TTS: Startup Failed");
            }

            // Notify if status changed
            if (SpeechServiceStatus != previousStatus || IsSpeechServiceHealthy != previousHealthy)
            {
                Console.WriteLine($"📊 Health status changed: {SpeechServiceStatus}");
                _logger.LogInformation("Health status changed: {Status}", SpeechServiceStatus);
                OnHealthStatusChanged(new HealthStatusChangedEventArgs
                {
                    ServiceName = "PiperTts",
                    IsHealthy = IsSpeechServiceHealthy,
                    StatusMessage = SpeechServiceStatus
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Speech health check failed with exception: {ex.Message}");
            _logger.LogError(ex, "Speech health check failed with exception");
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