// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR Hub for real-time photo upload notifications.
/// Notifies all connected clients (e.g., TrainsPage) when a photo is uploaded via MAUI.
/// </summary>
public class PhotoHub : Hub
{
    /// <summary>
    /// Called when a new photo is uploaded.
    /// Broadcasts to all connected clients.
    /// </summary>
    public async Task NotifyPhotoUploadedAsync(string category, Guid entityId, string photoPath)
    {
        await Clients.All.SendAsync("PhotoUploaded", new
        {
            category,
            entityId,
            photoPath,
            uploadedAt = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
