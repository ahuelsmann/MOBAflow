// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>
/// Platform-neutral contract for real-time photo notifications from the REST API hub.
/// </summary>
public interface IPhotoHubClient : IAsyncDisposable
{
    event Func<string, DateTime, Task>? PhotoUploaded;

    event Func<string, Guid, DateTime, Task>? PhotoDeleted;

    bool IsConnected { get; }

    Task ConnectAsync(string serverIp, int serverPort);

    Task DisconnectAsync();
}