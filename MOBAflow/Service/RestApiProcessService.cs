// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// Starts the standalone MOBApi project process when "Auto-start REST API" is enabled.
/// WinUI then uses MOBApi for status, clients, and MAUI discovery instead of hosting its own in-process API.
/// </summary>
public sealed class RestApiProcessService : IDisposable
{
    private const string SolutionFileName = "Moba.slnx";
    /// <summary>Folder and assembly name under output (must match <c>MOBApi.csproj</c> <c>AssemblyName</c>).</summary>
    private const string RestApiProjectName = "MOBApi";

    /// <summary>
    /// Raised when the REST API has been detected as reachable (either already running or just started).
    /// Subscribers can refresh the status UI immediately instead of waiting for the next poll.
    /// </summary>
    public event EventHandler<int>? ApiBecameReachable;

    private readonly AppSettings _appSettings;
    private readonly ILogger<RestApiProcessService> _logger;
    private readonly ILogger<UdpDiscoveryResponder> _discoveryLogger;
    private Process? _process;
    private UdpDiscoveryResponder? _udpResponder;
    private bool _disposed;
    private readonly SemaphoreSlim _startLock = new(1, 1);

    public RestApiProcessService(
        AppSettings appSettings,
        ILogger<RestApiProcessService> logger,
        ILogger<UdpDiscoveryResponder> discoveryLogger)
    {
        _appSettings = appSettings;
        _logger = logger;
        _discoveryLogger = discoveryLogger;
    }

    /// <summary>
    /// True when the MOBApi process has been started and not yet stopped.
    /// </summary>
    public bool IsRunning => _process != null && !_process.HasExited;

    /// <summary>
    /// Starts the MOBApi project process when "Auto-start REST API" is enabled.
    /// Idempotent: if our process is already running, returns immediately.
    /// Thread-safe: only one process is started even if StartAsync is called concurrently (e.g. from App and PostStartup).
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _startLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsRunning)
                return;

            var port = _appSettings.RestApi.Port > 0 ? _appSettings.RestApi.Port : 5001;

            // If API is already reachable (e.g. run standalone), do not start a second process
            if (await IsApiReachableAsync(port, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("MOBApi already running on port {Port} – reusing existing process", port);
                StartDiscoveryResponder(port);
                ApiBecameReachable?.Invoke(this, port);
                return;
            }

            // Ensure Windows Firewall allows UDP discovery (21106) and REST API (TCP port) so MAUI can connect
            FirewallHelper.EnsureFirewallRulesExist(port);

            // Prefer MOBApi next to WinUI (copied by build); fall back to repo-root convention
            var (dllPath, workingDir, usePreBuilt) = ResolveMobaApiPaths();
            if (dllPath == null || workingDir == null)
            {
                _logger.LogWarning("MOBApi not found – build WinUI to copy MOBApi output, or run from repo with MOBApi built");
                return;
            }

            string fileName;
            string arguments;
            if (usePreBuilt)
            {
                fileName = "dotnet";
                arguments = $"\"{dllPath}\" --urls \"http://0.0.0.0:{port}\"";
                _logger.LogDebug("Starting MOBApi from {Path}", dllPath);
            }
            else
            {
                fileName = "dotnet";
                arguments = $"run --project \"{dllPath}\" --urls \"http://0.0.0.0:{port}\"";
                _logger.LogInformation("MOBApi not yet built – running dotnet run (first start may be slow)");
            }

            try
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        WorkingDirectory = workingDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    },
                    EnableRaisingEvents = true
                };
                // Discovery runs in WinUI so MAUI can find the server (same as former in-process setup)
                _process.StartInfo.EnvironmentVariables["MOBAFLOW_DISCOVERY_IN_WINUI"] = "1";
                if (!string.IsNullOrWhiteSpace(_appSettings.Application.PhotoStoragePath))
                    _process.StartInfo.EnvironmentVariables["MOBAFLOW_PHOTOS_PATH"] = _appSettings.Application.PhotoStoragePath.Trim();

                _process.Exited += (sender, _) =>
                {
                    if (sender is not Process p)
                        return;

                    try
                    {
                        if (!p.HasExited)
                            return;

                        var exitCode = p.ExitCode;
                        if (exitCode != 0)
                            _logger.LogWarning("MOBApi process exited with code {ExitCode}", exitCode);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogDebug(ex, "MOBApi process exit state not yet available");
                    }
                };

                _process.Start();
                _logger.LogInformation("MOBApi process started (port {Port}), PID {Pid}", port, _process.Id);

                StartDiscoveryResponder(port);

                // Wait for the REST API to become reachable (poll up to 30s) so WinUI continues only when the server is ready
                const int pollIntervalMs = 300;
                const int maxWaitMs = 30_000;
                var waited = 0;
                while (waited < maxWaitMs && !_process.HasExited)
                {
                    await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
                    waited += pollIntervalMs;
                    if (await IsApiReachableAsync(port, cancellationToken).ConfigureAwait(false))
                    {
                        _logger.LogInformation("MOBApi became reachable after {Ms}ms", waited);
                        ApiBecameReachable?.Invoke(this, port);
                        break;
                    }
                }
                if (!await IsApiReachableAsync(port, cancellationToken).ConfigureAwait(false) && !_process.HasExited)
                    _logger.LogWarning("MOBApi not yet reachable after {Ms}ms – continuing anyway", waited);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start MOBApi process");
                _process?.Dispose();
                _process = null;
            }
        }
        finally
        {
            _startLock.Release();
        }
    }

    private void StartDiscoveryResponder(int port)
    {
        try
        {
            _udpResponder?.Stop();
            _udpResponder?.Dispose();
            _udpResponder = new UdpDiscoveryResponder(_discoveryLogger, port);
            _udpResponder.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UDP Discovery responder could not start");
        }
    }

    /// <summary>
    /// Returns true if the MOBApi status endpoint responds successfully on the given port.
    /// </summary>
    private static async Task<bool> IsApiReachableAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = await client.GetAsync(
                $"http://127.0.0.1:{port}/api/status",
                cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Stops the MOBApi process if running.
    /// </summary>
    public void Stop()
    {
        try
        {
            _udpResponder?.Stop();
            _udpResponder?.Dispose();
            _udpResponder = null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error stopping UDP Discovery responder");
        }

        if (_process == null)
            return;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                if (!_process.WaitForExit(TimeSpan.FromSeconds(2)))
                    _logger.LogDebug("MOBApi process did not exit within 2s");
                _logger.LogInformation("MOBApi process stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping MOBApi process");
        }
        finally
        {
            _process?.Dispose();
            _process = null;
        }
    }

    private static string? FindRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, SolutionFileName)))
                return dir;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    /// <summary>
    /// Resolves path to MOBApi.dll or MOBApi.csproj and working directory.
    /// Prefers MOBApi next to the running app (build-copied); falls back to repo-root convention.
    /// Returns (pathToDllOrCsproj, workingDir, usePreBuilt: true if path is DLL, false if path is .csproj).
    /// </summary>
    private static (string? path, string? workingDir, bool usePreBuilt) ResolveMobaApiPaths()
    {
        // Prefer MOBApi next to WinUI (see MOBAflow.csproj CopyMOBAapiToOutput target)
        var appDir = AppContext.BaseDirectory;
        var localDll = Path.Combine(appDir, RestApiProjectName, $"{RestApiProjectName}.dll");
        if (File.Exists(localDll))
        {
            var workingDir = Path.GetDirectoryName(localDll);
            return (localDll, workingDir!, true);
        }

        var repoRoot = FindRepositoryRoot();
        if (string.IsNullOrEmpty(repoRoot))
            return (null, null, false);

        var projectPath = Path.Combine(repoRoot, RestApiProjectName, $"{RestApiProjectName}.csproj");
        var buildOutputDir = Path.Combine(repoRoot, RestApiProjectName, "bin", "Debug", "net10.0");
        var repoDll = Path.Combine(buildOutputDir, $"{RestApiProjectName}.dll");

        if (File.Exists(repoDll))
            return (repoDll, buildOutputDir, true);
        if (File.Exists(projectPath))
            return (projectPath, repoRoot, false);

        return (null, null, false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _startLock.Dispose();
        Stop();
    }
}
