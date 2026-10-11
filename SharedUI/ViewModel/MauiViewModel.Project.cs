// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// MOBAsmart project selection: the phone controls one project of the synchronized solution and connects to that
/// project's Z21. In MOBAflow every project runs with its own Z21.
/// </summary>
public sealed partial class MauiViewModel
{
    private bool _isRefreshingProjectPicker;

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

        // A project picked on the phone runs locally unless MOBAflow runs it; MOBAflow's runtime is not changed.
        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime != true)
        {
            RunInBackground(
                _runtimeConnection.ActivateProjectAsync(project.Model, _applicationLifetimeCts.Token),
                "Activate the selected project");
        }
    }

    private void RefreshProjectPicker()
    {
        var projects = _projectContext?.SolutionViewModel?.Projects;
        _isRefreshingProjectPicker = true;
        try
        {
            ProjectNames = projects is null ? [] : [.. projects.Select(project => project.Name)];
            var selected = _projectContext?.SelectedProject;
            SelectedProjectIndex = projects is not null && selected is not null
                ? projects.IndexOf(selected)
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
    private async Task ApplySelectedProjectZ21Async()
    {
        var address = _projectContext?.SelectedProject?.Model.Z21.IpAddress.Trim() ?? string.Empty;
        if (string.Equals(address, Z21IpAddress, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _uiDispatcher.InvokeOnUiAsync(() =>
        {
            Z21IpAddress = address;
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        if (_runtimeSnapshots.Current.IsConnected)
        {
            await _runtimeConnection.DisconnectAsync(_applicationLifetimeCts.Token).ConfigureAwait(false);
        }

        if (address.Length > 0)
        {
            await _uiDispatcher.InvokeOnUiAsync(() => ConnectCommand.ExecuteAsync(null)).ConfigureAwait(false);
        }
    }
}
