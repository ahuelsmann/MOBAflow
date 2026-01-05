// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

/// <summary>
/// Real-time SignalR Client for photo notifications.
/// Connects to WinUI REST Server's PhotoHub for instant photo updates.
/// Uses proper Microsoft.AspNetCore.SignalR.Client (not WebSocket).
/// </summary>
public class PhotoHubClient : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly ILogger<PhotoHubClient>? _logger;

    // Events for real-time photo updates
    public event Func<string, DateTime, Task>? PhotoUploaded; // photoPath, uploadedAt
    public event Func<string, Guid, DateTime, Task>? PhotoDeleted;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public PhotoHubClient(ILogger<PhotoHubClient>? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Connect to the SignalR PhotoHub.
    /// </summary>
    public async Task ConnectAsync(string serverIp, int serverPort)
    {
        try
        {
            var hubUrl = $"http://{serverIp}:{serverPort}/photos-hub";
            _logger?.LogInformation("🔗 Connecting to PhotoHub: {HubUrl}", hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                })
                .Build();

            // Register server event handlers  
            _hubConnection.On<string, DateTime>("PhotoUploaded", OnPhotoUploaded);
            _hubConnection.On<string, Guid, DateTime>("PhotoDeleted", OnPhotoDeleted);

            await _hubConnection.StartAsync();
            _logger?.LogInformation("✅ Connected to PhotoHub successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Failed to connect to PhotoHub");
            throw;
        }
    }

    /// <summary>
    /// Disconnect from the SignalR Hub.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                _logger?.LogInformation("✅ Disconnected from PhotoHub");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disconnecting from PhotoHub");
            }
        }
    }

    /// <summary>
    /// Handle PhotoUploaded event from SignalR Hub (real-time).
    /// </summary>
    private async Task OnPhotoUploaded(string photoPath, DateTime uploadedAt)
    {
        try
        {
            _logger?.LogInformation("📸 [REAL-TIME] Photo uploaded: {PhotoPath}", photoPath);

            if (PhotoUploaded != null)
                await PhotoUploaded.Invoke(photoPath, uploadedAt);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling PhotoUploaded event");
        }
    }

    /// <summary>
    /// Handle PhotoDeleted event from SignalR Hub (real-time).
    /// </summary>
    private async Task OnPhotoDeleted(string category, Guid entityId, DateTime deletedAt)
    {
        try
        {
            _logger?.LogInformation("🗑️ [REAL-TIME] Photo deleted: {Category}/{EntityId}", category, entityId);

            if (PhotoDeleted != null)
                await PhotoDeleted.Invoke(category, entityId, deletedAt);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling PhotoDeleted event");
        }
    }

    /// <summary>
    /// Cleanup resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await DisconnectAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        GC.SuppressFinalize(this);
    }
}
