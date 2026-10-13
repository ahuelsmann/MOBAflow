// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Protocol;

using CommunityToolkit.Mvvm.ComponentModel;

using Domain;

using Microsoft.Extensions.Logging;

/// <summary>
/// MOBAsmart project selection: the phone controls one project of the synchronized solution and connects to that
/// project's Z21. In MOBAflow every project runs with its own Z21.
/// </summary>
public sealed partial class MauiViewModel
{
    private bool _isRefreshingProjectPicker;
    private int _z21Port = Z21Protocol.DefaultPort;
    private readonly Lock _projectSelectionSync = new();
    private Task _projectSelectionTask = Task.CompletedTask;
    private long _projectSelectionVersion;
    private Guid? _localProjectId;

    /// <summary>Gets the names of the projects of the synchronized solution, in solution order.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> ProjectNames { get; private set; }

    /// <summary>Gets or sets the position of the project this phone controls in <see cref="ProjectNames"/>; -1 when none.</summary>
    [ObservableProperty]
    public partial int SelectedProjectIndex { get; set; }

    partial void OnSelectedProjectIndexChanged(int value)
    {
        if (_isRefreshingProjectPicker
            || _projectContext?.SolutionViewModel?.Projects is not { } projects
            || value < 0
            || value >= projects.Count)
        {
            return;
        }

        var project = projects[value];
        _projectContext.SelectedProject = project;
    }

    private void RefreshProjectPicker()
    {
        var selectedProject = _projectContext?.SelectedProject;
        var projects = _projectContext?.SolutionViewModel?.Projects;
        _isRefreshingProjectPicker = true;
        try
        {
            ProjectNames = projects is null ? [] : [.. projects.Select(project => project.Name)];
            SelectedProjectIndex = projects is not null && selectedProject is not null
                ? projects.IndexOf(selectedProject)
                : -1;
        }
        finally
        {
            _isRefreshingProjectPicker = false;
        }
    }

    /// <summary>
    /// Connects to the selected project's Z21; a connection to the Z21 of the previously selected project is closed.
    /// </summary>
    private Task QueueProjectSelection()
    {
        lock (_projectSelectionSync)
        {
            var version = ++_projectSelectionVersion;
            _projectSelectionTask = ApplySelectedProjectAsync(
                _projectSelectionTask, _projectContext?.SelectedProject?.Model, version);
            return _projectSelectionTask;
        }
    }

    private bool IsCurrentProjectSelection(long version) =>
        !_appLifecycle.IsStopping && version == Volatile.Read(ref _projectSelectionVersion);

    private async Task ApplySelectedProjectAsync(Task previous, Project? project, long version)
    {
        // Publish this request as the queue tail before callbacks can enqueue another selection.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        await previous.ConfigureAwait(false);
        if (!IsCurrentProjectSelection(version)) return;
        try
        {
            var cancellationToken = _applicationLifetimeCts.Token;
            var address = project?.Z21.IpAddress.Trim() ?? string.Empty;
            var port = project?.Z21.Port is > 0 ? project.Z21.Port : Z21Protocol.DefaultPort;
            var endpointChanged = !string.Equals(address, Z21IpAddress, StringComparison.OrdinalIgnoreCase)
                || port != _z21Port;
            var projectChanged = project?.Id != _localProjectId;

            // Always log off on a handoff, including an unconfirmed UDP session. Do not stop the old layout.
            if (endpointChanged || projectChanged)
            {
                await _runtimeConnection.DisconnectForProjectSwitchAsync(cancellationToken).ConfigureAwait(false);
                if (!IsCurrentProjectSelection(version)) return;
            }

            await SelectRemoteProjectForSelectionAsync(project, cancellationToken).ConfigureAwait(false);
            if (!IsCurrentProjectSelection(version)) return;

            await RefreshLocalProjectForSelectionAsync(project, projectChanged, cancellationToken).ConfigureAwait(false);
            if (!IsCurrentProjectSelection(version)) return;

            await PublishSelectedProjectEndpointAsync(address, port, version).ConfigureAwait(false);

            if (IsCurrentProjectSelection(version) && (endpointChanged || projectChanged) && address.Length > 0)
                await _runtimeConnection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            await ReportProjectSelectionFailureAsync(ex, version).ConfigureAwait(false);
        }
    }

    private async Task SelectRemoteProjectForSelectionAsync(Project? project, CancellationToken cancellationToken)
    {
        if (project is null || _runtimeHubRemoteClient is null) return;
        try
        {
            await _runtimeHubRemoteClient.SelectProjectAsync(project.Id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException and not AccessViolationException)
        {
            // Remote registration can fail during a network interruption; the local handoff still owns its project.
            LogRemoteProjectSelectionFailed(_logger, ex);
        }
    }

    private async Task RefreshLocalProjectForSelectionAsync(Project? project, bool projectChanged, CancellationToken cancellationToken)
    {
        if (project is null)
        {
            _localProjectId = null;
            return;
        }
        if (projectChanged)
            await _runtimeConnection.ActivateProjectAsync(project, cancellationToken).ConfigureAwait(false);
        else
            await _runtimeConnection.UpdateProjectAsync(project, cancellationToken).ConfigureAwait(false);
        _localProjectId = project.Id;
    }

    private Task PublishSelectedProjectEndpointAsync(string address, int port, long version) =>
        _uiDispatcher.InvokeOnUiAsync(() =>
        {
            if (IsCurrentProjectSelection(version))
            {
                Z21IpAddress = address;
                _z21Port = port;
            }
            return Task.CompletedTask;
        });

    private async Task ReportProjectSelectionFailureAsync(Exception exception, long version)
    {
        // Each request completes so a later selection can recover after a failed handoff.
        if (exception is OperationCanceledException) return;
        LogProjectSelectionFailed(_logger, exception);
        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            if (IsCurrentProjectSelection(version)) Z21ConnectionStatus = $"Project selection failed: {exception.Message}";
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Project selection failed")]
    private static partial void LogProjectSelectionFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Remote project selection failed; continuing the local project handoff")]
    private static partial void LogRemoteProjectSelectionFailed(ILogger logger, Exception exception);
}
