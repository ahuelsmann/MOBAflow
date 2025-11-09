using Microsoft.AspNetCore.SignalR;
using Moba.Backend.Monitor;

namespace Moba.Backend.Hub;

/// <summary>
/// SignalR Hub for broadcasting real-time feedback events to connected clients (e.g., mobile apps).
/// </summary>
public class FeedbackHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly FeedbackMonitor _monitor;

    public FeedbackHub(FeedbackMonitor monitor)
    {
        _monitor = monitor;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// Sends current statistics to the newly connected client.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        System.Diagnostics.Debug.WriteLine($"✅ Client connected: {Context.ConnectionId}");

        // Send current statistics to the new client
        var stats = _monitor.GetAllStatistics();
        System.Diagnostics.Debug.WriteLine($"📤 Sending {stats.Count} initial statistics to {Context.ConnectionId}");
        await Clients.Caller.SendAsync("InitialStatistics", stats);
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        System.Diagnostics.Debug.WriteLine($"❌ Client disconnected: {Context.ConnectionId}, Reason: {exception?.Message ?? "Normal"}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to request current statistics.
    /// </summary>
    /// <returns>List of all feedback statistics</returns>
    public async Task<List<FeedbackStatistics>> GetStatistics()
    {
        return await Task.FromResult(_monitor.GetAllStatistics());
    }

    /// <summary>
    /// Allows clients to reset all statistics.
    /// </summary>
    public async Task ResetAllStatistics()
    {
        _monitor.ResetAll();
        await Clients.All.SendAsync("StatisticsReset");
    }

    /// <summary>
    /// Allows clients to reset statistics for a specific InPort.
    /// </summary>
    /// <param name="inPort">The InPort to reset</param>
    public async Task ResetStatistics(uint inPort)
    {
        _monitor.Reset(inPort);
        await Clients.All.SendAsync("StatisticReset", inPort);
    }
}
