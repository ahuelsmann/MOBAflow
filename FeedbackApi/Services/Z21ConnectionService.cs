using Microsoft.Extensions.Hosting;
using Moba.Backend;
using Moba.Backend.Manager;
using System.Net;

namespace FeedbackApi.Services;

/// <summary>
/// Background service that automatically connects to the Z21 when FeedbackApi starts.
/// This allows the mobile app to receive real-time feedback without requiring the WinUI app to run.
/// </summary>
public class Z21ConnectionService : IHostedService
{
    private readonly Z21 _z21;
    private readonly FeedbackMonitorManager _feedbackMonitorManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Z21ConnectionService> _logger;

    public Z21ConnectionService(
        Z21 z21,
        FeedbackMonitorManager feedbackMonitorManager,
        IConfiguration configuration,
        ILogger<Z21ConnectionService> logger)
    {
        _z21 = z21;
        _feedbackMonitorManager = feedbackMonitorManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get Z21 IP address from configuration
            var z21Ip = _configuration["Z21:IpAddress"] ?? "192.168.0.111";
            var address = IPAddress.Parse(z21Ip);

            _logger.LogInformation("🔌 Connecting to Z21 at {IpAddress}...", z21Ip);

            await _z21.ConnectAsync(address, cancellationToken);

            _logger.LogInformation("✅ Connected to Z21 at {IpAddress}", z21Ip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to connect to Z21");
            // Don't throw - allow the API to start even if Z21 connection fails
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🔌 Disconnecting from Z21...");

            _feedbackMonitorManager.Dispose();
            await _z21.DisconnectAsync();

            _logger.LogInformation("✅ Disconnected from Z21");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error disconnecting from Z21");
        }
    }
}
