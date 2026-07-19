// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Microsoft.Extensions.Logging;

/// <summary>
/// Runs the application shutdown sequence exactly once.
/// </summary>
internal sealed class WinUiShutdownCoordinator
{
    private readonly Func<Task> _prepareApplicationAsync;
    private readonly Func<ValueTask> _disposeServicesAsync;
    private readonly Action _exitApplication;
    private readonly ILogger<WinUiShutdownCoordinator> _logger;
    private readonly object _syncRoot = new();
    private Task? _shutdownTask;

    public WinUiShutdownCoordinator(
        Func<Task> prepareApplicationAsync,
        Func<ValueTask> disposeServicesAsync,
        Action exitApplication,
        ILogger<WinUiShutdownCoordinator> logger)
    {
        _prepareApplicationAsync = prepareApplicationAsync ?? throw new ArgumentNullException(nameof(prepareApplicationAsync));
        _disposeServicesAsync = disposeServicesAsync ?? throw new ArgumentNullException(nameof(disposeServicesAsync));
        _exitApplication = exitApplication ?? throw new ArgumentNullException(nameof(exitApplication));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ShutdownAsync()
    {
        lock (_syncRoot)
        {
            return _shutdownTask ??= ShutdownCoreAsync();
        }
    }

    private async Task ShutdownCoreAsync()
    {
        _logger.LogInformation("Application shutdown started");

        try
        {
            await _prepareApplicationAsync();
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
    }
}
