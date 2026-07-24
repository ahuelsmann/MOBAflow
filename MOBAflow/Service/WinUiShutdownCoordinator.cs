// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.Extensions.Logging;

/// <summary>
/// Runs the application shutdown sequence exactly once.
/// </summary>
internal sealed class WinUiShutdownCoordinator
{
    private readonly Func<Task<bool>> _prepareApplicationAsync;
    private readonly Func<ValueTask> _disposeServicesAsync;
    private readonly Action _exitApplication;
    private readonly ILogger<WinUiShutdownCoordinator> _logger;
    private readonly object _syncRoot = new();
    private Task<bool>? _shutdownTask;
    private bool _shutdownCompleted;

    public WinUiShutdownCoordinator(
        Func<Task<bool>> prepareApplicationAsync,
        Func<ValueTask> disposeServicesAsync,
        Action exitApplication,
        ILogger<WinUiShutdownCoordinator> logger)
    {
        _prepareApplicationAsync = prepareApplicationAsync ?? throw new ArgumentNullException(nameof(prepareApplicationAsync));
        _disposeServicesAsync = disposeServicesAsync ?? throw new ArgumentNullException(nameof(disposeServicesAsync));
        _exitApplication = exitApplication ?? throw new ArgumentNullException(nameof(exitApplication));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> ShutdownAsync()
    {
        lock (_syncRoot)
        {
            if (_shutdownTask is { IsCompleted: true } && !_shutdownCompleted)
            {
                _shutdownTask = null;
            }

            return _shutdownTask ??= ShutdownCoreAsync();
        }
    }

    private async Task<bool> ShutdownCoreAsync()
    {
        _logger.LogInformation("Application shutdown started");

        try
        {
            if (!await _prepareApplicationAsync())
            {
                _logger.LogInformation("Application shutdown cancelled");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Application shutdown preparation failed");
        }

        try
        {
            await _disposeServicesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Service disposal during application shutdown failed");
        }

        _exitApplication();
        lock (_syncRoot)
        {
            _shutdownCompleted = true;
        }

        return true;
    }
}
