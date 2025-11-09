using Microsoft.AspNetCore.SignalR;
using Moba.Backend.Hub;
using Moba.Backend.Monitor;

namespace FeedbackApi.Services;

/// <summary>
/// Background service that listens to FeedbackMonitor events and broadcasts them via SignalR.
/// </summary>
public class FeedbackBroadcastService : IHostedService
{
    private readonly FeedbackMonitor _monitor;
    private readonly IHubContext<FeedbackHub> _hubContext;

    public FeedbackBroadcastService(FeedbackMonitor monitor, IHubContext<FeedbackHub> hubContext)
    {
        _monitor = monitor;
        _hubContext = hubContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Subscribe to feedback events
        _monitor.FeedbackReceived += OnFeedbackReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Unsubscribe from feedback events
        _monitor.FeedbackReceived -= OnFeedbackReceived;
        return Task.CompletedTask;
    }

    private async void OnFeedbackReceived(object? sender, FeedbackStatistics stats)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"📡 Broadcasting FeedbackUpdate: InPort={stats.InPort}, Count={stats.TotalCount}");
            
            // Broadcast to all connected clients
            await _hubContext.Clients.All.SendAsync("FeedbackUpdate", stats);
            
            System.Diagnostics.Debug.WriteLine($"✅ Broadcast complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Broadcast error: {ex.Message}");
        }
    }
}
