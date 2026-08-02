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
public sealed class PhotoHub(IControlPlaneHubConnectionRegistry connectionRegistry) : Hub
{
    public override async Task OnConnectedAsync()
    {
        connectionRegistry.RegisterReadConnection(Context);
        if (Context.User?.Identity?.IsAuthenticated != true &&
            await connectionRegistry.EvaluateAnonymousReadAsync(
                    CompatibilityReadTransport.SignalR,
                    Context.ConnectionAborted)
                .ConfigureAwait(false) == CompatibilityReadDecision.UpgradeRequired)
        {
            connectionRegistry.Unregister(Context);
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        connectionRegistry.Unregister(Context);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
