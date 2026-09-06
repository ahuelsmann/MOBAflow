#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Events;
using Moba.WinUI.Service;

internal sealed partial class RestApiStatusServiceTests
{
    [Test]
    public async Task RefreshAsync_ShouldRestorePhotoNotifications_WhenHubReconnectAttemptsAreExhausted()
    {
        var state = new PhotoTestState();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSignalR();
        builder.Services.AddSingleton(state);
        builder.Services.AddTransient(_ => new PhotoTestHub(state));
        var server = builder.Build();
        await using var serverLifetime = server.ConfigureAwait(false);
        server.Use(async (context, next) =>
        {
            if (state.IsUnavailable && context.Request.Path.StartsWithSegments("/photos-hub", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.OnCompleted(() =>
                {
                    state.RecordRejectedReconnect();
                    return Task.CompletedTask;
                });
                return;
            }

            await next(context).ConfigureAwait(false);
        });
        server.MapGet("/api/status", () => Results.Json(new { status = "running" }));
        server.MapHub<PhotoTestHub>("/photos-hub");
        await server.StartAsync().ConfigureAwait(false);

        var port = new Uri(server.Urls.Single()).Port;
        var photoClient = new PhotoHubClient(NullLogger<PhotoHubClient>.Instance);
        await using var photoClientLifetime = photoClient.ConfigureAwait(false);
        using var httpHandler = new PhotoTestHttpHandler { UseProxy = false };
        var dependencies = CreateDependencies(httpHandler, photoClient, port);
        await using var dependenciesLifetime = dependencies.ConfigureAwait(false);
        var initialPhoto = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recoveredPhoto = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        dependencies.EventBus.Subscribe<PhotoAssignedEvent>(photo =>
        {
            if (photo.PhotoPath == "photos/initial.jpg") initialPhoto.TrySetResult(photo.PhotoPath);
            if (photo.PhotoPath == "photos/recovered.jpg") recoveredPhoto.TrySetResult(photo.PhotoPath);
        });

        await dependencies.StatusService.RefreshAsync().ConfigureAwait(false);
        Assert.That(await initialPhoto.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false),
            Is.EqualTo("photos/initial.jpg"));

        state.InterruptConnections();
        await state.ReconnectAttemptsRejected.Task.WaitAsync(TimeSpan.FromSeconds(45)).ConfigureAwait(false);
        state.Restore();

        using var recoveryTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var refreshTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            // Exercise the public refresh path while the client finishes its last failed reconnect.
            do
            {
                await dependencies.StatusService.RefreshAsync().WaitAsync(recoveryTimeout.Token).ConfigureAwait(false);
                if (recoveredPhoto.Task.IsCompleted) break;
            }
            while (await refreshTimer.WaitForNextTickAsync(recoveryTimeout.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (recoveryTimeout.IsCancellationRequested)
        {
            Assert.Fail("Photo notifications did not resume after the hub became available again.");
        }

        Assert.That(await recoveredPhoto.Task.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false),
            Is.EqualTo("photos/recovered.jpg"));
    }

    private sealed partial class PhotoTestHttpHandler : HttpClientHandler;

    private sealed class PhotoTestState
    {
        private readonly ConcurrentDictionary<string, Action> _connections = new();
        private int _unavailable;
        private int _rejectedReconnects;
        private int _restored;

        public bool IsUnavailable => Volatile.Read(ref _unavailable) != 0;
        public string PhotoPath => Volatile.Read(ref _restored) == 0 ? "photos/initial.jpg" : "photos/recovered.jpg";
        public TaskCompletionSource ReconnectAttemptsRejected { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void AddConnection(string id, Action abort) => _connections[id] = abort;
        public void RemoveConnection(string id) => _connections.TryRemove(id, out _);

        public void InterruptConnections()
        {
            Volatile.Write(ref _unavailable, 1);
            foreach (var abort in _connections.Values) abort();
        }

        public void RecordRejectedReconnect()
        {
            if (Interlocked.Increment(ref _rejectedReconnects) == 4) ReconnectAttemptsRejected.TrySetResult();
        }

        public void Restore()
        {
            Volatile.Write(ref _restored, 1);
            Volatile.Write(ref _unavailable, 0);
        }
    }

    private sealed partial class PhotoTestHub(PhotoTestState state) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Abort the transport, not the Hub: a deliberate Hub abort disables automatic reconnect.
            state.AddConnection(Context.ConnectionId, Context.GetHttpContext()!.Abort);
            await Clients.Caller.SendAsync("PhotoUploaded", state.PhotoPath, DateTime.UtcNow, Context.ConnectionAborted)
                .ConfigureAwait(false);
            await base.OnConnectedAsync().ConfigureAwait(false);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            state.RemoveConnection(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
#endif