// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Domain;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;

using Moba.Common.Runtime;

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

/// <summary>
/// End-to-end tests against the built MOBApi, started once for this fixture as an isolated process.
/// The tests run in order against the same server, so a snapshot pushed earlier is cached for later tests.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("LiveE2E")]
internal sealed class RuntimeHubLiveE2ETests
{
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(10);
    private static readonly Guid ProjectId = Guid.Parse("99999999-8888-7777-6666-555555555555");
    private MobaApiProcess? _server;

    private string BaseUrl => _server?.BaseUri.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("The MOBApi process is not running.");

    [OneTimeSetUp]
    public async Task StartServerAsync()
    {
        _server = await MobaApiProcess.StartAsync(MobaApiProcess.GetAvailablePort()).ConfigureAwait(false);
    }

    [OneTimeTearDown]
    public async Task StopServerAsync()
    {
        if (_server is not null)
        {
            await _server.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test, Order(1)]
    public async Task RuntimeHub_Should_ForwardSnapshotAndLocomotiveDriveCommand()
    {
        var testSnapshot = new MobaRuntimeSnapshot
        {
            ProjectId = ProjectId,
            IsConnected = true,
            IsTrackPowerOn = true,
            StatusText = "E2E connected",
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    Name = "E2E Signal",
                    Kind = SignalBoxElementKind.Signal
                }
            ]
        };
        var snapshotReceived = new TaskCompletionSource<MobaRuntimeSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var driveReceived = new TaskCompletionSource<(int Address, int Speed, bool Forward)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sessionStateReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await PublishSolutionAsync().ConfigureAwait(false);
        await using var hostHub = await ConnectHostHubAsync(driveReceived).ConfigureAwait(false);
        await using var remoteHub = await ConnectRemoteHubAsync(snapshotReceived, sessionStateReceived, testSnapshot.StatusText).ConfigureAwait(false);

        await hostHub
            .InvokeAsync(RuntimeHubMethods.PushSnapshot, RuntimeJsonSerializer.Serialize(testSnapshot))
            .ConfigureAwait(false);

        var receivedSnapshot = await snapshotReceived.Task.WaitAsync(StepTimeout).ConfigureAwait(false);
        Assert.That(receivedSnapshot.IsConnected, Is.True);
        Assert.That(receivedSnapshot.SignalBoxElements, Has.Count.EqualTo(1));
        Assert.That(receivedSnapshot.SignalBoxElements[0].Name, Is.EqualTo("E2E Signal"));

        var sessionOperational = await sessionStateReceived.Task.WaitAsync(StepTimeout).ConfigureAwait(false);
        Assert.That(sessionOperational, Is.True);

        await remoteHub
            .InvokeAsync(RuntimeHubMethods.SetLocomotiveDrive, ProjectId.ToString(), 3, 42, true)
            .ConfigureAwait(false);

        var drive = await driveReceived.Task.WaitAsync(StepTimeout).ConfigureAwait(false);
        Assert.That(drive.Address, Is.EqualTo(3));
        Assert.That(drive.Speed, Is.EqualTo(42));
        Assert.That(drive.Forward, Is.True);
    }

    [Test, Order(3)]
    public async Task RegisterRemote_Should_DeliverCachedSnapshot_WithoutNewPush()
    {
        var cachedSnapshot = new MobaRuntimeSnapshot
        {
            ProjectId = ProjectId,
            IsConnected = true,
            StatusText = "Cached before remote connect",
            SignalBoxElements =
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = Guid.Parse("22222222-3333-4444-5555-666666666666"),
                    Name = "Cached Signal",
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Hp0
                }
            ]
        };

        await using var hostHub = await ConnectHostHubAsync(
            new TaskCompletionSource<(int Address, int Speed, bool Forward)>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ConfigureAwait(false);

        await hostHub
            .InvokeAsync(RuntimeHubMethods.PushSnapshot, RuntimeJsonSerializer.Serialize(cachedSnapshot))
            .ConfigureAwait(false);

        var snapshotReceived = new TaskCompletionSource<MobaRuntimeSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sessionStateReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var remoteHub = await ConnectRemoteHubAsync(snapshotReceived, sessionStateReceived, cachedSnapshot.StatusText).ConfigureAwait(false);

        var receivedSnapshot = await snapshotReceived.Task.WaitAsync(StepTimeout).ConfigureAwait(false);
        Assert.That(receivedSnapshot.StatusText, Is.EqualTo("Cached before remote connect"));
        Assert.That(receivedSnapshot.SignalBoxElements[0].Name, Is.EqualTo("Cached Signal"));
    }

    [Test, Order(2)]
    public async Task RestFallback_Should_RoundtripSnapshotAndCommands()
    {

        await PublishSolutionAsync().ConfigureAwait(false);
        using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var snapshot = new MobaRuntimeSnapshot
        {
            ProjectId = ProjectId,
            IsConnected = true,
            StatusText = "REST E2E"
        };

        using (var putContent = new StringContent(RuntimeJsonSerializer.Serialize(snapshot), Encoding.UTF8, "application/json"))
        {
            var putResponse = await http.PutAsync("/api/runtime/snapshot", putContent).ConfigureAwait(false);
            Assert.That(putResponse.IsSuccessStatusCode, Is.True, await putResponse.Content.ReadAsStringAsync());
        }

        var metaResponse = await http.GetAsync($"/api/runtime/meta?projectId={ProjectId}").ConfigureAwait(false);
        Assert.That(metaResponse.IsSuccessStatusCode, Is.True);
        var meta = await metaResponse.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
        Assert.That(meta.GetProperty("isConnected").GetBoolean(), Is.True);

        var getSnapshotResponse = await http.GetAsync($"/api/runtime/snapshot?projectId={ProjectId}").ConfigureAwait(false);
        Assert.That(getSnapshotResponse.IsSuccessStatusCode, Is.True);
        var restored = RuntimeJsonSerializer.Deserialize(await getSnapshotResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
        Assert.That(restored?.StatusText, Is.EqualTo("REST E2E"));

        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var commandBody = JsonSerializer.Serialize(new { projectId = ProjectId, signalId, aspect = SignalAspect.Hp0.ToString() });
        using (var postContent = new StringContent(commandBody, Encoding.UTF8, "application/json"))
        {
            var postResponse = await http.PostAsync("/api/runtime/commands/signal-aspect", postContent).ConfigureAwait(false);
            Assert.That((int)postResponse.StatusCode, Is.EqualTo(StatusCodes.Status202Accepted));
        }

        var pendingResponse = await http.GetAsync("/api/runtime/commands/pending").ConfigureAwait(false);
        Assert.That(pendingResponse.IsSuccessStatusCode, Is.True);
        var pending = await pendingResponse.Content.ReadFromJsonAsync<RuntimeCommandEnvelope>().ConfigureAwait(false);
        Assert.That(pending, Is.Not.Null);
        Assert.That(pending!.Type, Is.EqualTo(RuntimeCommandType.SetSignalAspect));
        Assert.That(pending.SignalId, Is.EqualTo(signalId));
        Assert.That(pending.SignalAspect, Is.EqualTo(SignalAspect.Hp0));
        Assert.That(pending.ProjectId, Is.EqualTo(ProjectId));

        var emptyPending = await http.GetAsync("/api/runtime/commands/pending").ConfigureAwait(false);
        Assert.That(emptyPending.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));
    }

    /// <summary>Synchronizes a solution that contains <see cref="ProjectId"/>; commands name known projects only.</summary>
    private async Task PublishSolutionAsync()
    {
        using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var solutionJson =
            $$"""{"name":"E2E","schemaVersion":{{Solution.CurrentSchemaVersion}},"projects":[{"id":"{{ProjectId}}","name":"E2E"}]}""";
        using var content = new StringContent(solutionJson, Encoding.UTF8, "application/json");
        using var response = await http.PutAsync(new Uri("/api/solution", UriKind.Relative), content).ConfigureAwait(false);
        Assert.That(response.IsSuccessStatusCode, Is.True, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
    }

    private async Task<HubConnection> ConnectHostHubAsync(
        TaskCompletionSource<(int Address, int Speed, bool Forward)> driveReceived)
    {
        var hub = new HubConnectionBuilder()
            .WithUrl($"{BaseUrl}/runtime-hub")
            .Build();

        hub.On<string, int, int, bool>(RuntimeHubMethods.ExecuteSetLocomotiveDrive, (_, address, speed, forward) =>
        {
            driveReceived.TrySetResult((address, speed, forward));
            return Task.CompletedTask;
        });

        await hub.StartAsync().ConfigureAwait(false);
        await hub.InvokeAsync(RuntimeHubMethods.RegisterHost).ConfigureAwait(false);
        return hub;
    }

    private async Task<HubConnection> ConnectRemoteHubAsync(
        TaskCompletionSource<MobaRuntimeSnapshot> snapshotReceived,
        TaskCompletionSource<bool> sessionStateReceived,
        string expectedStatusText)
    {
        var hub = new HubConnectionBuilder()
            .WithUrl($"{BaseUrl}/runtime-hub")
            .Build();

        hub.On<string>(RuntimeHubMethods.SnapshotUpdated, json =>
        {
            var snapshot = RuntimeJsonSerializer.Deserialize(json);
            if (snapshot?.StatusText == expectedStatusText)
            {
                snapshotReceived.TrySetResult(snapshot);
            }

            return Task.CompletedTask;
        });

        hub.On<bool>(RuntimeHubMethods.SessionStateChanged, isOperational =>
        {
            if (isOperational)
            {
                sessionStateReceived.TrySetResult(true);
            }

            return Task.CompletedTask;
        });

        await hub.StartAsync().ConfigureAwait(false);
        await hub.InvokeAsync(RuntimeHubMethods.RegisterRemote, "e2e-test-client", ProjectId.ToString()).ConfigureAwait(false);
        return hub;
    }
}
