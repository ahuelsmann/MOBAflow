// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.RestApi.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR Hub for real-time photo upload notifications.
/// WinUI PhotoHubClient subscribes to "PhotoUploaded" (photoPath, uploadedAt) to assign the photo to the selected item.
/// </summary>
public sealed class PhotoHub : Hub
{
}
