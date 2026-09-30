// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Moba.Common.Discovery;
using Moba.Common.Runtime;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Channels;

/// <summary>
/// Starts the real MOBApi process and verifies the plain-HTTP contract used by MOBAflow and MOBAsmart:
/// discovery health, credential-free commands and reads, and SignalR reconnection after a server restart.
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class MobApiProcessTests
{
    [Test]
    [CancelAfter(45_000)]
    public async Task HealthEndpoint_ShouldMatchMobAsmartDiscoveryContract()
    {
        var server = await MobaApiProcess.StartAsync(GetAvailablePort()).ConfigureAwait(false);
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
        var server = await MobaApiProcess.StartAsync(GetAvailablePort()).ConfigureAwait(false);
        await using var serverLifetime = server.ConfigureAwait(false);

        using var accepted = await server
            .SendAsync(
                HttpMethod.Post,
                "api/runtime/commands/locomotive/drive",
                JsonContent.Create(new { address = 3, speed = 40, forward = true }))
            .ConfigureAwait(false);
        using var pending = await server.SendAsync(HttpMethod.Get, "api/runtime/commands/pending").ConfigureAwait(false);
        var command = await pending.Content.ReadFromJsonAsync<RuntimeCommandEnvelope>().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(pending.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(command?.Type, Is.EqualTo(RuntimeCommandType.SetLocomotiveDrive));
            Assert.That(command?.LocomotiveAddress, Is.EqualTo(3));
        }
    }

    [Test]
    [CancelAfter(120_000)]
    public async Task Reads_ShouldRemainEquivalent_AfterReconnectAndServerRestart()
    {
        var port = GetAvailablePort();
        SignalRReconnectProbe? reconnectProbe = null;
        try
        {
            await using (var firstServer = await MobaApiProcess.StartAsync(port))
            {
                var firstSnapshot = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { IsConnected = true });
                await firstServer.PublishSnapshotAsync(firstSnapshot);

                var restSnapshot = await firstServer.ReadSnapshotAsync();
                reconnectProbe = await SignalRReconnectProbe.StartAsync(firstServer.HubUri);
                var firstSignalRSnapshot = await reconnectProbe.WaitForSnapshotAsync(firstSnapshot);

                Assert.That(firstSignalRSnapshot, Is.EqualTo(restSnapshot));
            }

            await using (var restartedServer = await MobaApiProcess.StartAsync(port))
            {
                await reconnectProbe.WaitForReconnectAsync();
                var restartedSnapshot = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { IsConnected = false });
                await restartedServer.PublishSnapshotAsync(restartedSnapshot);

                var restSnapshot = await restartedServer.ReadSnapshotAsync();
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

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private sealed class MobaApiProcess : IAsyncDisposable
    {
        private readonly HttpClient _client;
        private readonly Process _process;

        private MobaApiProcess(Process process, HttpClient client)
        {
            _process = process;
            _client = client;
        }

        public Uri HubUri => new(_client.BaseAddress!, "runtime-hub");

        public static async Task<MobaApiProcess> StartAsync(int httpPort)
        {
            var assemblyPath = ResolveAssemblyPath();
            var output = new List<string>();
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = Path.GetDirectoryName(assemblyPath)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add(assemblyPath);
            startInfo.Environment["MOBAFLOW_DISCOVERY_IN_WINUI"] = "1";
            startInfo.Environment["MOBAFLOW_HTTP_PORT"] = httpPort.ToString(
                System.Globalization.CultureInfo.InvariantCulture);

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, args) => RecordOutput(output, args.Data);
            process.ErrorDataReceived += (_, args) => RecordOutput(output, args.Data);
            var client = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{httpPort}/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("MOBApi process did not start.");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var startupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await WaitUntilReachableAsync(client, process, output, startupTimeout.Token);
                return new MobaApiProcess(process, client);
            }
            catch (Exception exception)
            {
                client.Dispose();
                await StopProcessAsync(process);
                process.Dispose();
                throw new InvalidOperationException(
                    $"MOBApi process startup failed. Output: {FormatOutput(output)}",
                    exception);
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content = null)
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            return await _client.SendAsync(request).ConfigureAwait(false);
        }

        public async Task PublishSnapshotAsync(string snapshotJson)
        {
            using var response = await SendAsync(
                HttpMethod.Put,
                "api/runtime/snapshot",
                new StringContent(snapshotJson, Encoding.UTF8, "application/json"));
            await EnsureSuccessAsync(response);
        }

        public async Task<string> ReadSnapshotAsync()
        {
            using var response = await SendAsync(HttpMethod.Get, "api/runtime/snapshot");
            await EnsureSuccessAsync(response);
            return await response.Content.ReadAsStringAsync();
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await StopProcessAsync(_process);
            _process.Dispose();
        }

        private static string ResolveAssemblyPath()
        {
            // The SDK stamps the build configuration into the test assembly; output folder names differ per target.
            var configuration = typeof(MobApiProcessTests).Assembly
                .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
                ?? throw new InvalidOperationException("Unable to determine the test build configuration.");
            var assemblyPath = Path.Combine(FindRepositoryRoot(), "MOBApi", "bin", configuration, "net10.0", "MOBApi.dll");
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("Build MOBApi before running the process integration test.", assemblyPath);
            return assemblyPath;
        }

        private static async Task WaitUntilReachableAsync(
            HttpClient client,
            Process process,
            List<string> output,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"MOBApi exited with code {process.ExitCode}. Output: {FormatOutput(output)}");
                }

                try
                {
                    using var response = await client.GetAsync(MobApiHealthProbe.HealthPath, cancellationToken);
                    if (response.IsSuccessStatusCode)
                        return;
                }
                catch (HttpRequestException)
                {
                    // Kestrel is still starting.
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"MOBApi returned {(int)response.StatusCode} ({response.StatusCode}): {body}");
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
                    return directory.FullName;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate the MOBAflow repository root.");
        }

        private static void RecordOutput(List<string> output, string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;
            lock (output)
            {
                output.Add(line);
                if (output.Count > 100)
                    output.RemoveAt(0);
            }
        }

        private static string FormatOutput(List<string> output)
        {
            lock (output)
                return output.Count == 0 ? "(not captured)" : string.Join(Environment.NewLine, output);
        }

        private static async Task StopProcessAsync(Process process)
        {
            try
            {
                if (process.HasExited)
                    return;

                process.Kill(entireProcessTree: true);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the state check and shutdown.
            }
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
                await connection.InvokeAsync(RuntimeHubMethods.RegisterRemote, ClientId);
                probe._reconnected.TrySetResult();
            };

            await connection.StartAsync();
            await connection.InvokeAsync(RuntimeHubMethods.RegisterRemote, ClientId);
            return probe;
        }

        public Task WaitForReconnectAsync() =>
            _reconnected.Task.WaitAsync(TimeSpan.FromSeconds(30));

        public async Task<string> RegisterAndWaitForSnapshotAsync(string expectedSnapshot)
        {
            await _connection.InvokeAsync(RuntimeHubMethods.RegisterRemote, ClientId);
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
