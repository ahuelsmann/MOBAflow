// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Moba.Common.Discovery;
using Moba.Common.Runtime;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;

/// <summary>
/// Starts the real MOBApi process and verifies the plain-HTTP contract used by MOBAflow and MOBAsmart:
/// discovery health, credential-free commands and reads, and SignalR reconnection after a server restart.
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class MobApiProcessTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    [CancelAfter(45_000)]
    public async Task HealthEndpoint_ShouldMatchMobAsmartDiscoveryContract()
    {
        var server = await MobaApiProcess.StartAsync(MobaApiProcess.GetAvailablePort()).ConfigureAwait(false);
        await using var serverLifetime = server.ConfigureAwait(false);

        using var health = await server.SendAsync(HttpMethod.Get, MobApiHealthProbe.HealthPath).ConfigureAwait(false);
        var body = await health.Content.ReadAsStringAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(MobApiHealthProbe.IsHealthyResponse(body), Is.True);
        }
    }

    [Test]
    [CancelAfter(45_000)]
    public async Task RemoteCommand_ShouldBeAcceptedAndQueued_WithoutCredentials()
    {
        var server = await MobaApiProcess.StartAsync(MobaApiProcess.GetAvailablePort()).ConfigureAwait(false);
        await using var serverLifetime = server.ConfigureAwait(false);

        await server.PublishSolutionAsync(ProjectId).ConfigureAwait(false);
        using var accepted = await server
            .SendAsync(
                HttpMethod.Post,
                "api/runtime/commands/locomotive/drive",
                JsonContent.Create(new { projectId = ProjectId, address = 3, speed = 40, forward = true }))
            .ConfigureAwait(false);
        using var pending = await server.SendAsync(HttpMethod.Get, "api/runtime/commands/pending").ConfigureAwait(false);
        var command = await pending.Content.ReadFromJsonAsync<RuntimeCommandEnvelope>().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(pending.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(command?.Type, Is.EqualTo(RuntimeCommandType.SetLocomotiveDrive));
            Assert.That(command?.LocomotiveAddress, Is.EqualTo(3));
            Assert.That(command?.ProjectId, Is.EqualTo(ProjectId));
        }
    }

    [Test]
    [CancelAfter(45_000)]
    public async Task RemoteCommand_WithoutProject_IsRejected()
    {
        var server = await MobaApiProcess.StartAsync(MobaApiProcess.GetAvailablePort()).ConfigureAwait(false);
        await using var serverLifetime = server.ConfigureAwait(false);
        await server.PublishSolutionAsync(ProjectId).ConfigureAwait(false);

        using var rejected = await server
            .SendAsync(
                HttpMethod.Post,
                "api/runtime/commands/locomotive/drive",
                JsonContent.Create(new { address = 3, speed = 40, forward = true }))
            .ConfigureAwait(false);
        using var pending = await server.SendAsync(HttpMethod.Get, "api/runtime/commands/pending").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(pending.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
    }

    [Test]
    [CancelAfter(120_000)]
    public async Task Reads_ShouldRemainEquivalent_AfterReconnectAndServerRestart()
    {
        var port = MobaApiProcess.GetAvailablePort();
        SignalRReconnectProbe? reconnectProbe = null;
        try
        {
            await using (var firstServer = await MobaApiProcess.StartAsync(port))
            {
                var firstSnapshot = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = ProjectId, IsConnected = true });
                await firstServer.PublishSnapshotAsync(firstSnapshot);

                var restSnapshot = await firstServer.ReadSnapshotAsync(ProjectId);
                reconnectProbe = await SignalRReconnectProbe.StartAsync(firstServer.HubUri);
                var firstSignalRSnapshot = await reconnectProbe.WaitForSnapshotAsync(firstSnapshot);

                Assert.That(firstSignalRSnapshot, Is.EqualTo(restSnapshot));
            }

            await using (var restartedServer = await MobaApiProcess.StartAsync(port))
            {
                await reconnectProbe.WaitForReconnectAsync();
                var restartedSnapshot = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = ProjectId, IsConnected = false });
                await restartedServer.PublishSnapshotAsync(restartedSnapshot);

                var restSnapshot = await restartedServer.ReadSnapshotAsync(ProjectId);
                var signalRSnapshot = await reconnectProbe.RegisterAndWaitForSnapshotAsync(restartedSnapshot);

                Assert.That(signalRSnapshot, Is.EqualTo(restSnapshot));
            }
        }
        finally
        {
            if (reconnectProbe is not null)
                await reconnectProbe.DisposeAsync();
        }
    }

    private sealed class SignalRReconnectProbe : IAsyncDisposable
    {
        private const string ClientId = "process-integration-client";
        private readonly HubConnection _connection;
        private readonly TaskCompletionSource _reconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Channel<string> _snapshots = Channel.CreateUnbounded<string>();

        private SignalRReconnectProbe(HubConnection connection)
        {
            _connection = connection;
        }

        public static async Task<SignalRReconnectProbe> StartAsync(Uri hubUrl)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => options.Transports = HttpTransportType.LongPolling)
                .WithAutomaticReconnect(
                [
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                ])
                .Build();
            var probe = new SignalRReconnectProbe(connection);
            connection.On<string>(RuntimeHubMethods.SnapshotUpdated, snapshot =>
                probe._snapshots.Writer.TryWrite(snapshot));
            connection.Reconnected += async _ =>
            {
                await connection.InvokeAsync(RuntimeHubMethods.RegisterRemote, ClientId, ProjectId.ToString());
                probe._reconnected.TrySetResult();
            };

            await connection.StartAsync();
            await connection.InvokeAsync(RuntimeHubMethods.RegisterRemote, ClientId, ProjectId.ToString());
            return probe;
        }

        public Task WaitForReconnectAsync() =>
            _reconnected.Task.WaitAsync(TimeSpan.FromSeconds(30));

        public async Task<string> RegisterAndWaitForSnapshotAsync(string expectedSnapshot)
        {
            await _connection.InvokeAsync(RuntimeHubMethods.RegisterRemote, ClientId, ProjectId.ToString());
            return await WaitForSnapshotAsync(expectedSnapshot);
        }

        public async Task<string> WaitForSnapshotAsync(string expectedSnapshot)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (await _snapshots.Reader.WaitToReadAsync(timeout.Token))
            {
                while (_snapshots.Reader.TryRead(out var snapshot))
                {
                    if (string.Equals(snapshot, expectedSnapshot, StringComparison.Ordinal))
                        return snapshot;
                }
            }

            throw new InvalidOperationException("The SignalR connection completed before the expected snapshot arrived.");
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
