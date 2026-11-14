// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;
using SharedUI.ViewModel;

/// <summary>
/// The host work behind the main window: REST API status polling, speech health checks, the runtime observers
/// no shell surface binds, and the shutdown of all of them. The window forwards its lifecycle events here and
/// keeps only its own visuals and subscriptions. The composition root hands over the shell's shutdown workflow,
/// so this service does not depend on the shell.
/// </summary>
internal sealed partial class MainWindowLifecycleService
{
    private readonly Func<Action, Task<bool>> _shellShutdownWorkflow;
    private readonly SystemStatusViewModel _systemStatus;
    private readonly IReadOnlyList<IDisposable> _runtimeObservers;
    private readonly RestApiStatusService _restApiStatusService;
    private readonly RestApiProcessService _restApiProcessService;
    private readonly HealthCheckService _healthCheckService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger<MainWindowLifecycleService> _logger;
    private bool _statusPollingStarted;
    private bool _healthChecksStarted;

    /// <summary>Creates the lifecycle owner for the host services of the main window.</summary>
    /// <param name="shellShutdownWorkflow">
    /// The shell's shutdown workflow. It saves unsaved changes, invokes the given release action once shutdown is
    /// confirmed and before the runtimes stop, and returns <see langword="false"/> when the operator cancelled.
    /// </param>
    /// <param name="systemStatus">Shows the speech health in the status bar.</param>
    /// <param name="runtimeObservers">
    /// Host-owned states that follow the session and the runtime without a shell binding (counters, traffic
    /// monitor, Solution page, signal-box projection); they are disposed once shutdown is confirmed.
    /// </param>
    /// <param name="restApiStatusService">Polls the REST API status while the window is active.</param>
    /// <param name="restApiProcessService">Owns the MOBApi process that stops with the window.</param>
    /// <param name="healthCheckService">Checks the selected speech provider periodically.</param>
    /// <param name="uiDispatcher">Marshals health results, which arrive on timer threads, to the UI thread.</param>
    /// <param name="logger">Logs shutdown steps that failed.</param>
    public MainWindowLifecycleService(
        Func<Action, Task<bool>> shellShutdownWorkflow,
        SystemStatusViewModel systemStatus,
        IReadOnlyList<IDisposable> runtimeObservers,
        RestApiStatusService restApiStatusService,
        RestApiProcessService restApiProcessService,
        HealthCheckService healthCheckService,
        IUiDispatcher uiDispatcher,
        ILogger<MainWindowLifecycleService> logger)
    {
        ArgumentNullException.ThrowIfNull(shellShutdownWorkflow);
        ArgumentNullException.ThrowIfNull(systemStatus);
        ArgumentNullException.ThrowIfNull(runtimeObservers);
        ArgumentNullException.ThrowIfNull(restApiStatusService);
        ArgumentNullException.ThrowIfNull(restApiProcessService);
        ArgumentNullException.ThrowIfNull(healthCheckService);
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(logger);
        _shellShutdownWorkflow = shellShutdownWorkflow;
        _systemStatus = systemStatus;
        _runtimeObservers = runtimeObservers;
        _restApiStatusService = restApiStatusService;
        _restApiProcessService = restApiProcessService;
        _healthCheckService = healthCheckService;
        _uiDispatcher = uiDispatcher;
        _logger = logger;
    }

    /// <summary>Starts REST API status polling once the deferred UI startup of the window ran.</summary>
    public void StartStatusPolling()
    {
        _restApiStatusService.Start();
        _statusPollingStarted = true;
    }

    /// <summary>The window became active: resumes REST API status polling once it was started.</summary>
    public void ResumeStatusPolling()
    {
        if (_statusPollingStarted)
        {
            _restApiStatusService.ResumePolling();
        }
    }

    /// <summary>The window became inactive: pauses REST API status polling once it was started.</summary>
    public void PauseStatusPolling()
    {
        if (_statusPollingStarted)
        {
            _restApiStatusService.PausePolling();
        }
    }

    /// <summary>
    /// Starts the periodic speech health checks and shows their results in the status bar. Further calls do nothing.
    /// </summary>
    public void InitializeHealthChecks()
    {
        if (_healthChecksStarted)
        {
            return;
        }

        _healthChecksStarted = true;
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.StartPeriodicChecks();

        _uiDispatcher.InvokeOnUi(() => _systemStatus.UpdateHealthStatus(_healthCheckService.SpeechServiceStatus));
    }

    /// <summary>
    /// Runs the shell's shutdown workflow, which saves unsaved changes and releases the runtimes; the runtime
    /// observers are released once shutdown is confirmed and before the runtimes stop.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when the operator cancelled, so the window stays open; otherwise
    /// <see langword="true"/>, also when the workflow failed, because a failed workflow must not keep the window open.
    /// </returns>
    public async Task<bool> ConfirmShutdownAsync()
    {
        try
        {
            // The window continues on the UI thread: it detaches its own events and bindings afterwards.
            return await _shellShutdownWorkflow(ReleaseRuntimeObservers).ConfigureAwait(true);
        }
        catch (Exception ex) when (LogFailure(LogShellShutdownFailed, ex))
        {
            // Logged by the filter; closing continues.
            return true;
        }
    }

    /// <summary>
    /// Stops the host services after the window detached its own events: runtime observers, REST API status
    /// polling, the MOBApi process and the speech health checks. A failing step is logged and the next one runs.
    /// </summary>
    public async Task StopAsync()
    {
        // Normally already released before the runtimes stopped; Dispose is idempotent and covers a failed shell workflow.
        ReleaseRuntimeObservers();

        _restApiStatusService.Stop();

        try
        {
            await _restApiStatusService.DisposeAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (LogFailure(LogStatusServiceDisposeFailed, ex))
        {
            // Logged by the filter; the remaining services still stop.
        }

        try
        {
            _restApiProcessService.Stop();
        }
        catch (Exception ex) when (LogFailure(LogProcessServiceStopFailed, ex))
        {
            // Logged by the filter; the remaining services still stop.
        }

        if (!_healthChecksStarted)
        {
            return;
        }

        _healthCheckService.HealthStatusChanged -= OnHealthStatusChanged;
        _healthCheckService.StopPeriodicChecks();
        try
        {
            _healthCheckService.Dispose();
        }
        catch (Exception ex) when (LogFailure(LogHealthCheckDisposeFailed, ex))
        {
            // Logged by the filter; nothing else is left to stop.
        }
    }

    /// <summary>
    /// The shell confirmed shutdown; the counter, traffic monitor, Solution page and signal-box projection stop
    /// following the session and the runtime.
    /// </summary>
    private void ReleaseRuntimeObservers()
    {
        foreach (var observer in _runtimeObservers)
        {
            observer.Dispose();
        }
    }

    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        _uiDispatcher.InvokeOnUi(() => _systemStatus.UpdateHealthStatus(e.StatusMessage));
    }

    /// <summary>Logs a failed shutdown step from an exception filter, so every step failure is handled alike.</summary>
    private bool LogFailure(Action<ILogger, Exception> log, Exception exception)
    {
        log(_logger, exception);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "ViewModel shutdown workflow failed")]
    private static partial void LogShellShutdownFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RestApiStatusService.DisposeAsync failed")]
    private static partial void LogStatusServiceDisposeFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RestApiProcessService.Stop failed")]
    private static partial void LogProcessServiceStopFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HealthCheckService.Dispose failed")]
    private static partial void LogHealthCheckDisposeFailed(ILogger logger, Exception exception);
}
