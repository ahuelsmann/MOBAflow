// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Moba.Common.Runtime;
using Moba.Common.Security;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Security;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

[TestFixture]
[NonParallelizable]
internal sealed class AuthenticatedControlPlaneProcessTests
{
    [Test]
    [CancelAfter(120_000)]
    public async Task CompatibilityStatus_Should_BeHostOnlyAndExposeBoundedEvidence()
    {
        var storageDirectory = Path.Combine(
            Path.GetTempPath(),
            "MOBAflow",
            "authenticated-control-plane-tests",
            Guid.NewGuid().ToString("N"));
        var httpPort = GetAvailablePort();
        var httpsPort = GetAvailablePort();
        while (httpsPort == httpPort)
            httpsPort = GetAvailablePort();

        Directory.CreateDirectory(storageDirectory);
        try
        {
            var server = await MobaApiProcess
                .StartAsync(storageDirectory, httpPort, httpsPort)
                .ConfigureAwait(false);
            await using (server.ConfigureAwait(false))
            {
                using var compatibilityRead = await server
                    .SendAnonymousAsync(HttpMethod.Get, "api/runtime/snapshot")
                    .ConfigureAwait(false);
                using var anonymousStatus = await server
                    .SendAnonymousAsync(HttpMethod.Get, "api/control-plane/security/compatibility")
                    .ConfigureAwait(false);
                using var hostStatus = await server
                    .SendHostAsync(HttpMethod.Get, "api/control-plane/security/compatibility")
                    .ConfigureAwait(false);
                var responseBody = await hostStatus.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var body = JsonDocument.Parse(responseBody);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(compatibilityRead.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                    Assert.That(anonymousStatus.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                    Assert.That(hostStatus.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(responseBody, Does.Not.Contain("token").IgnoreCase);
                    Assert.That(responseBody, Does.Not.Contain("snapshot").IgnoreCase);
                    Assert.That(responseBody, Does.Not.Contain("hardware").IgnoreCase);
                    Assert.That(
                        body.RootElement.GetProperty("telemetry").GetProperty("outcomes").GetArrayLength(),
                        Is.GreaterThanOrEqualTo(1));
                    Assert.That(body.RootElement.TryGetProperty("readiness", out _), Is.True);
                }
            }
        }
        finally
        {
            if (Directory.Exists(storageDirectory))
                Directory.Delete(storageDirectory, recursive: true);
        }
    }

    [Test]
    [CancelAfter(120_000)]
    public async Task AuthenticatedReads_ShouldRemainEquivalent_AfterReconnectAndServerRestart()
    {
        var storageDirectory = Path.Combine(
            Path.GetTempPath(),
            "MOBAflow",
            "authenticated-control-plane-tests",
            Guid.NewGuid().ToString("N"));
        var httpPort = GetAvailablePort();
        var httpsPort = GetAvailablePort();
        while (httpsPort == httpPort)
            httpsPort = GetAvailablePort();

        Directory.CreateDirectory(storageDirectory);
        SignalRReconnectProbe? reconnectProbe = null;
        try
        {
            TokenResponse deviceToken;
            string fingerprint;
            await using (var firstServer = await MobaApiProcess.StartAsync(
                             storageDirectory,
                             httpPort,
                             httpsPort))
            {
                fingerprint = firstServer.PublicKeyFingerprint;
                deviceToken = await firstServer.PairReadOnlyClientAsync();
                var firstSnapshot = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { IsConnected = true });
                await firstServer.PublishSnapshotAsync(firstSnapshot);

                var restSnapshot = await firstServer.ReadSnapshotAsync(deviceToken.AccessToken);
                reconnectProbe = await firstServer.CreateSignalRReconnectProbeAsync(deviceToken.AccessToken);
                var firstSignalRSnapshot = await reconnectProbe.WaitForSnapshotAsync(firstSnapshot);

                Assert.That(firstSignalRSnapshot, Is.EqualTo(restSnapshot));
            }

            await using (var restartedServer = await MobaApiProcess.StartAsync(
                             storageDirectory,
                             httpPort,
                             httpsPort))
            {
                Assert.That(restartedServer.PublicKeyFingerprint, Is.EqualTo(fingerprint));
                await reconnectProbe.WaitForReconnectAsync();
                deviceToken = await restartedServer.RefreshAsync(deviceToken);
                var restartedSnapshot = RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { IsConnected = false });
                await restartedServer.PublishSnapshotAsync(restartedSnapshot);

                var restSnapshot = await restartedServer.ReadSnapshotAsync(deviceToken.AccessToken);
                var signalRSnapshot = await reconnectProbe.RegisterAndWaitForSnapshotAsync(restartedSnapshot);

                Assert.That(signalRSnapshot, Is.EqualTo(restSnapshot));
            }
        }
        finally
        {
            if (reconnectProbe is not null)
                await reconnectProbe.DisposeAsync();
            if (Directory.Exists(storageDirectory))
                Directory.Delete(storageDirectory, recursive: true);
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
        private readonly List<string> _processOutput;

        private MobaApiProcess(
            Process process,
            HttpClient client,
            HostTokenResponse hostToken,
            HostBootstrapPipeResponse bootstrapResponse,
            List<string> processOutput)
        {
            _process = process;
            _client = client;
            HostToken = hostToken;
            PublicKeyFingerprint = bootstrapResponse.PublicKeyFingerprint;
            _processOutput = processOutput;
        }

        public HostTokenResponse HostToken { get; }

        public string PublicKeyFingerprint { get; }

        public static async Task<MobaApiProcess> StartAsync(
            string storageDirectory,
            int httpPort,
            int httpsPort)
        {
            var repositoryRoot = FindRepositoryRoot();
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
                ?? throw new InvalidOperationException("Unable to determine the test build configuration.");
            var assemblyPath = Path.Combine(
                repositoryRoot,
                "MOBApi",
                "bin",
                configuration,
                "net10.0",
                "MOBApi.dll");
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("Build MOBApi before running the process integration test.", assemblyPath);

            await using var bootstrapChannel = new HostBootstrapParentChannel();
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
            startInfo.Environment["MOBAFLOW_HTTPS_PORT"] = httpsPort.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            startInfo.Environment["ControlPlaneSecurity__StorageDirectory"] = storageDirectory;
            bootstrapChannel.Configure(startInfo);

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, args) => RecordOutput(output, args.Data);
            process.ErrorDataReceived += (_, args) => RecordOutput(output, args.Data);
            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("MOBApi process did not start.");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                bootstrapChannel.CompleteHandleTransfer();

                using var startupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var bootstrap = await bootstrapChannel.ExchangeAsync(startupTimeout.Token);
                var client = CreatePinnedClient(httpsPort, bootstrap.Response.PublicKeyFingerprint);
                try
                {
                    await WaitUntilReachableAsync(client, process, output, startupTimeout.Token);
                    var hostToken = await PostAndReadAsync<HostBootstrapRequest, HostTokenResponse>(
                        client,
                        "api/control-plane/host/bootstrap",
                        new HostBootstrapRequest(bootstrap.Secret),
                        accessToken: null,
                        startupTimeout.Token);
                    return new MobaApiProcess(process, client, hostToken, bootstrap.Response, output);
                }
                catch
                {
                    client.Dispose();
                    throw;
                }
            }
            catch (Exception exception)
            {
                await StopProcessAsync(process);
                process.Dispose();
                throw new InvalidOperationException(
                    $"MOBApi process startup failed. Output: {FormatOutput(output)}",
                    exception);
            }
        }

        public async Task<TokenResponse> PairReadOnlyClientAsync()
        {
            var window = await PostAndReadAsync<OpenPairingRequest, PairingWindowResult>(
                _client,
                "api/control-plane/security/pairing/open",
                new OpenPairingRequest(ControlPlaneRole.ReadOnly),
                HostToken.AccessToken,
                CancellationToken.None);
            var submission = await PostAndReadAsync<PairingSubmission, PairingSubmissionResult>(
                _client,
                "api/control-plane/pairing/submit",
                new PairingSubmission(
                    window.PairingSecret,
                    Guid.NewGuid().ToString("N"),
                    "Process integration client",
                    ControlPlaneRole.ReadOnly),
                accessToken: null,
                CancellationToken.None);
            Assert.That(submission.Status, Is.EqualTo(PairingSubmissionStatus.Accepted));
            Assert.That(submission.RequestId, Is.Not.Null.And.Not.Empty);
            Assert.That(submission.ClaimToken, Is.Not.Null.And.Not.Empty);

            using var approve = CreateRequest(
                HttpMethod.Post,
                $"api/control-plane/security/pairing/requests/{submission.RequestId}/approve",
                HostToken.AccessToken);
            using var approveResponse = await _client.SendAsync(approve);
            await EnsureSuccessAsync(approveResponse);

            return await PostAndReadAsync<PairingClaimRequest, TokenResponse>(
                _client,
                "api/control-plane/pairing/claim",
                new PairingClaimRequest(submission.RequestId!, submission.ClaimToken!),
                accessToken: null,
                CancellationToken.None);
        }

        public Task<TokenResponse> RefreshAsync(TokenResponse token) =>
            PostAndReadAsync<RefreshTokenRequest, TokenResponse>(
                _client,
                "api/control-plane/token/refresh",
                new RefreshTokenRequest(token.CredentialId, token.RefreshToken),
                accessToken: null,
                CancellationToken.None);

        public async Task PublishSnapshotAsync(string snapshotJson)
        {
            using var request = CreateRequest(
                HttpMethod.Put,
                "api/runtime/snapshot",
                HostToken.AccessToken,
                new StringContent(snapshotJson, Encoding.UTF8, "application/json"));
            using var response = await _client.SendAsync(request);
            await EnsureSuccessAsync(response);
        }

        public async Task<string> ReadSnapshotAsync(string accessToken)
        {
            using var request = CreateRequest(HttpMethod.Get, "api/runtime/snapshot", accessToken);
            using var response = await _client.SendAsync(request);
            await EnsureSuccessAsync(response);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<HttpResponseMessage> SendAnonymousAsync(HttpMethod method, string path)
        {
            using var request = CreateRequest(method, path, accessToken: null);
            return await _client.SendAsync(request).ConfigureAwait(false);
        }

        public async Task<HttpResponseMessage> SendHostAsync(HttpMethod method, string path)
        {
            using var request = CreateRequest(method, path, HostToken.AccessToken);
            return await _client.SendAsync(request).ConfigureAwait(false);
        }

        public Task<SignalRReconnectProbe> CreateSignalRReconnectProbeAsync(string accessToken)
        {
            return SignalRReconnectProbe.StartAsync(
                new Uri(_client.BaseAddress!, "runtime-hub"),
                accessToken,
                PublicKeyFingerprint);
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await StopProcessAsync(_process);
            _process.Dispose();
        }

        private static HttpClient CreatePinnedClient(int httpsPort, string fingerprint) => new(
            CreatePinnedHandler(fingerprint))
        {
            BaseAddress = new Uri($"https://127.0.0.1:{httpsPort}/"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static HttpClientHandler CreatePinnedHandler(string fingerprint) => new()
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                ServerCertificatePinning.Matches(certificate, fingerprint)
        };

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
                    using var response = await client.GetAsync("api/photos/health", cancellationToken);
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

        private static async Task<TResponse> PostAndReadAsync<TRequest, TResponse>(
            HttpClient client,
            string path,
            TRequest body,
            string? accessToken,
            CancellationToken cancellationToken)
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                path,
                accessToken,
                JsonContent.Create(body));
            using var response = await client.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken)
                ?? throw new InvalidDataException($"MOBApi returned no {typeof(TResponse).Name} payload.");
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string path,
            string? accessToken,
            HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, path) { Content = content };
            request.Headers.TryAddWithoutValidation(
                CompatibilityReadHeaders.ClientRelease,
                "MOBAsmart 1.0.0");
            if (!string.IsNullOrWhiteSpace(accessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return request;
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
                return string.Join(Environment.NewLine, output);
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

        public static async Task<SignalRReconnectProbe> StartAsync(
            Uri hubUrl,
            string accessToken,
            string fingerprint)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                    options.Headers[CompatibilityReadHeaders.ClientRelease] = "MOBAsmart 1.0.0";
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => MobaApiProcess.CreatePinnedHandler(fingerprint);
                })
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
