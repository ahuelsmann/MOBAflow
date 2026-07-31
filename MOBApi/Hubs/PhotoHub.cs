// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Moba.MOBApi.Security;

/// <summary>
/// SignalR Hub for real-time photo upload notifications.
/// WinUI PhotoHubClient subscribes to "PhotoUploaded" (photoPath, uploadedAt) to assign the photo to the selected item.
/// </summary>
[Authorize(Policy = ControlPlaneCapabilities.Read)]
public sealed class PhotoHub(IControlPlaneConnectionRevoker connectionRevoker) : Hub
{
    public override Task OnConnectedAsync()
    {
        ControlPlaneHubConnectionRegistration.Register(Context, connectionRevoker);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        connectionRevoker.Unregister(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
