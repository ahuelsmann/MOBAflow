// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Integration;

using Moba.Common.Discovery;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

/// <summary>
/// Starts the built MOBApi as an isolated process on a free local port and stops it on disposal.
/// A missing build or a server that does not start fails the test instead of skipping it.
/// </summary>
internal sealed class MobaApiProcess : IAsyncDisposable
{
    private readonly HttpClient _client;
    private readonly Process _process;

    private MobaApiProcess(Process process, HttpClient client)
    {
        _process = process;
        _client = client;
    }

    public Uri BaseUri => _client.BaseAddress!;

    public Uri HubUri => new(_client.BaseAddress!, "runtime-hub");

    public static int GetAvailablePort()
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
            await WaitUntilReachableAsync(client, process, output, startupTimeout.Token).ConfigureAwait(false);
            return new MobaApiProcess(process, client);
        }
        catch (Exception exception)
        {
            client.Dispose();
            await StopProcessAsync(process).ConfigureAwait(false);
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
            new StringContent(snapshotJson, Encoding.UTF8, "application/json")).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    public async Task PublishSolutionAsync(Guid projectId)
    {
        var solutionJson =
            $$"""{"name":"Test","schemaVersion":{{Moba.Domain.Solution.CurrentSchemaVersion}},"projects":[{"id":"{{projectId}}","name":"Layout"}]}""";
        using var response = await SendAsync(
            HttpMethod.Put,
            "api/solution",
            new StringContent(solutionJson, Encoding.UTF8, "application/json")).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    public async Task<string> ReadSnapshotAsync(Guid projectId)
    {
        using var response = await SendAsync(HttpMethod.Get, $"api/runtime/snapshot?projectId={projectId}").ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await StopProcessAsync(_process).ConfigureAwait(false);
        _process.Dispose();
    }

    private static string ResolveAssemblyPath()
    {
        // The SDK stamps the build configuration into the test assembly; output folder names differ per target.
        var configuration = typeof(MobaApiProcess).Assembly
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
                using var response = await client.GetAsync(MobApiHealthProbe.HealthPath, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
                // Kestrel is still starting.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and shutdown.
        }
    }
}
