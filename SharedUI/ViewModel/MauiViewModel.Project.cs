// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Protocol;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// MOBAsmart project selection: the phone controls one project of the synchronized solution and connects to that
/// project's Z21. In MOBAflow every project runs with its own Z21.
/// </summary>
public sealed partial class MauiViewModel
{
    private bool _isRefreshingProjectPicker;
    private int _z21Port = Z21Protocol.DefaultPort;

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
    private async Task ApplySelectedProjectZ21Async(bool activateProject)
    {
        var project = _projectContext?.SelectedProject?.Model;
        var address = project?.Z21.IpAddress.Trim() ?? string.Empty;
        var port = project?.Z21.Port is > 0 ? project.Z21.Port : Z21Protocol.DefaultPort;
        var endpointChanged = !string.Equals(address, Z21IpAddress, StringComparison.OrdinalIgnoreCase)
            || port != _z21Port;
        if (!endpointChanged && !activateProject)
        {
            return;
        }

        if (endpointChanged && (_runtimeSnapshots.Current.IsConnected || _runtimeSnapshots.Current.IsZ21Connecting))
        {
            await _runtimeConnection.DisconnectAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
        }

        // The phone's local feedback runtime follows the project even while the UI projects MOBAflow's snapshots.
        if (activateProject && project is not null)
        {
            await _runtimeConnection.ActivateProjectAsync(project, _applicationLifetimeCts.Token).ConfigureAwait(false);
        }

        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            Z21IpAddress = address;
            _z21Port = port;
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        if (endpointChanged && address.Length > 0)
        {
            await _uiDispatcher.InvokeOnUiAsync(() => ConnectCommand.ExecuteAsync(null)).ConfigureAwait(false);
        }
    }
}
