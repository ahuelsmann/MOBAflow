using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moba.Sound;
using Timer = System.Timers.Timer;

namespace Moba.WinUI.Service;

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
        var enabled = _configuration.GetValue<bool>("HealthCheck:Enabled", true);
        if (!enabled)
        {
            Console.WriteLine("ℹ️ Health checks disabled in configuration");
            _logger.LogInformation("Health checks disabled in configuration");
            return;
        }

        var intervalSeconds = _configuration.GetValue<int>("HealthCheck:IntervalSeconds", 60);
        Console.WriteLine($"🔄 Starting periodic health checks every {intervalSeconds} seconds");
        _logger.LogInformation("Starting periodic health checks every {Interval} seconds", intervalSeconds);

        _healthCheckTimer = new Timer(intervalSeconds * 1000);
        _healthCheckTimer.Elapsed += async (sender, e) => await PerformHealthCheckAsync();
        _healthCheckTimer.AutoReset = true;
        _healthCheckTimer.Start();

        // Perform initial check
        _ = PerformHealthCheckAsync();
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
                SpeechServiceStatus = "✅ Healthy";
                IsSpeechServiceHealthy = true;
                Console.WriteLine("✅ Azure Speech Service: Healthy");
            }
            else
            {
                SpeechServiceStatus = "❌ Unhealthy";
                IsSpeechServiceHealthy = false;
                Console.WriteLine("❌ Azure Speech Service: Unhealthy");
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
        if (_disposed) return;

        StopPeriodicChecks();
        _healthCheckTimer?.Dispose();
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
