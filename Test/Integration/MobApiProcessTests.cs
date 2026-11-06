// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Moba.Contracts.Runtime;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Moba.Contracts.Discovery;

/// <summary>
/// Starts the real MOBApi process and verifies the plain-HTTP contract used by MOBAflow and MOBAsmart:
/// discovery health, credential-free commands and reads, and SignalR reconnection after a server restart.
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class MobApiProcessTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly TestCaseData[] SolutionHeaderCases =
    [
        new TestCaseData(Array.Empty<string>(), null, null).SetName("SolutionHeaders_Missing"),
        new TestCaseData(new[] { "X-MOBAflow-Solution-Path: C:/Layouts/test.json" }, "C:/Layouts/test.json", null)
            .SetName("SolutionHeaders_PathOnly"),
        new TestCaseData(new[] { "X-MOBAflow-Active-Project: Station" }, null, "Station")
            .SetName("SolutionHeaders_ProjectOnly"),
        new TestCaseData(
            new[] { "X-MOBAflow-Solution-Path: C:/Layouts/test.json", "X-MOBAflow-Active-Project: Station" },
            "C:/Layouts/test.json", "Station").SetName("SolutionHeaders_Both"),
        new TestCaseData(
            new[] { "X-MOBAflow-Solution-Path: C:/Layouts/a,b.json", "X-MOBAflow-Active-Project: Station, Yard" },
            "C:/Layouts/a,b.json", "Station, Yard").SetName("SolutionHeaders_CommasRemainInOneValue"),
        new TestCaseData(
            new[]
            {
                "X-MOBAflow-Solution-Path: C:/Layouts/first.json", "X-MOBAflow-Solution-Path: C:/Layouts/second.json",
                "X-MOBAflow-Active-Project: First, Yard", "X-MOBAflow-Active-Project: Second"
            }, "C:/Layouts/first.json", "First, Yard").SetName("SolutionHeaders_RepeatedFirstWins"),
        new TestCaseData(new[] { "X-MOBAflow-Solution-Path:", "X-MOBAflow-Active-Project:" }, "", "")
            .SetName("SolutionHeaders_EmptyIsNotMissing"),
        // Kestrel removes HTTP optional whitespace before exposing header values to model binding.
        new TestCaseData(new[] { "X-MOBAflow-Solution-Path:   ", "X-MOBAflow-Active-Project:\t " }, "", "")
            .SetName("SolutionHeaders_WhitespaceOnTheWire"),
        new TestCaseData(
            new[]
            {
                "X-MOBAflow-Solution-Path:", "X-MOBAflow-Solution-Path: later",
                "X-MOBAflow-Active-Project:", "X-MOBAflow-Active-Project: later"
            }, "", "").SetName("SolutionHeaders_EmptyFirstWins")
    ];

    [TestCaseSource(nameof(SolutionHeaderCases))]
    [CancelAfter(45_000)]
    public async Task SolutionPut_PreservesHeaderMetadataAndRawJson(string[] headerLines, string? sourcePath, string? activeProject)
    {
        var server = await MobaApiProcess.StartAsync(MobaApiProcess.GetAvailablePort()).ConfigureAwait(false);
        await using var serverLifetime = server.ConfigureAwait(false);
        var json = $$"""
            {
              "name": "Layout \u00dc",
              "schemaVersion": {{Moba.Domain.Solution.CurrentSchemaVersion}},
              "projects": [{"id":"{{ProjectId}}", "name":"Body project"}]
            }
            """;

        var statusLine = await PutRawSolutionAsync(server.BaseUri, json, headerLines).ConfigureAwait(false);
        using var metadataResponse = await server.SendAsync(HttpMethod.Get, "api/solution/meta").ConfigureAwait(false);
        using var metadata = JsonDocument.Parse(await metadataResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
        using var solutionResponse = await server.SendAsync(HttpMethod.Get, "api/solution").ConfigureAwait(false);
        var cachedJson = await solutionResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statusLine, Is.EqualTo("HTTP/1.1 200 OK"));
            Assert.That(metadataResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(metadata.RootElement.GetProperty("sourcePath").GetString(), Is.EqualTo(sourcePath));
            Assert.That(metadata.RootElement.GetProperty("activeProjectName").GetString(), Is.EqualTo(activeProject));
            Assert.That(metadata.RootElement.GetProperty("firstProjectName").GetString(), Is.EqualTo(activeProject ?? "Body project"));
            Assert.That(solutionResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(cachedJson, Is.EqualTo(json));
        }
    }

    [TestCase("not json")]
    [TestCase("{}")]
    [CancelAfter(45_000)]
    public async Task SolutionPut_InvalidBodyDoesNotReplaceCachedSolutionOrMetadata(string invalidJson)
    {
        var server = await MobaApiProcess.StartAsync(MobaApiProcess.GetAvailablePort()).ConfigureAwait(false);
        await using var serverLifetime = server.ConfigureAwait(false);
        await server.PublishSolutionAsync(ProjectId).ConfigureAwait(false);
        using var originalSolution = await server.SendAsync(HttpMethod.Get, "api/solution").ConfigureAwait(false);
        var originalJson = await originalSolution.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var originalMetadata = await server.SendAsync(HttpMethod.Get, "api/solution/meta").ConfigureAwait(false);
        var originalMetaJson = await originalMetadata.Content.ReadAsStringAsync().ConfigureAwait(false);

        var statusLine = await PutRawSolutionAsync(server.BaseUri, invalidJson,
            ["X-MOBAflow-Solution-Path: rejected", "X-MOBAflow-Active-Project: rejected"]).ConfigureAwait(false);
        using var solution = await server.SendAsync(HttpMethod.Get, "api/solution").ConfigureAwait(false);
        using var metadata = await server.SendAsync(HttpMethod.Get, "api/solution/meta").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statusLine, Is.EqualTo("HTTP/1.1 400 Bad Request"));
            Assert.That(await solution.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo(originalJson));
            Assert.That(await metadata.Content.ReadAsStringAsync().ConfigureAwait(false), Is.EqualTo(originalMetaJson));
        }
    }

    private static async Task<string?> PutRawSolutionAsync(Uri baseUri, string json, string[] headerLines)
    {
        // Raw header lines avoid HttpClient joining repeated values before Kestrel receives them.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var client = new TcpClient();
        await client.ConnectAsync(baseUri.Host, baseUri.Port, timeout.Token).ConfigureAwait(false);
        var stream = client.GetStream();
        await using var streamLifetime = stream.ConfigureAwait(false);
        var request = new StringBuilder()
            .Append("PUT /api/solution HTTP/1.1\r\nHost: ").Append(baseUri.Authority)
            .Append("\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: ")
            .Append(Encoding.UTF8.GetByteCount(json).ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Append("\r\n");
        foreach (var line in headerLines)
            request.Append(line).Append("\r\n");
        request.Append("\r\n").Append(json);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(request.ToString()), timeout.Token).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        return await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
    }

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
            connection.On<string>(nameof(IRuntimeHubRemoteCallbacks.SnapshotUpdated), snapshot =>
                probe._snapshots.Writer.TryWrite(snapshot));
            connection.Reconnected += async _ =>
            {
                await connection.InvokeAsync(nameof(IRuntimeHubRemoteMethods.RegisterRemote), ClientId, ProjectId.ToString());
                probe._reconnected.TrySetResult();
            };

            await connection.StartAsync();
            await connection.InvokeAsync(nameof(IRuntimeHubRemoteMethods.RegisterRemote), ClientId, ProjectId.ToString());
            return probe;
        }

        public Task WaitForReconnectAsync() =>
            _reconnected.Task.WaitAsync(TimeSpan.FromSeconds(30));

        public async Task<string> RegisterAndWaitForSnapshotAsync(string expectedSnapshot)
        {
            await _connection.InvokeAsync(nameof(IRuntimeHubRemoteMethods.RegisterRemote), ClientId, ProjectId.ToString());
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
