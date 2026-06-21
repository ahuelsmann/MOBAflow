// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using Common.Events;

using CommunityToolkit.Mvvm.ComponentModel;

using System.IO;

/// <summary>
/// MainWindowViewModel - MOBAsmart sync diagnostics for Overview page.
/// </summary>
public partial class MainWindowViewModel
{
    private static readonly TimeSpan SyncFreshnessWindow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RestCacheTolerance = TimeSpan.FromSeconds(30);

    [ObservableProperty]
    private bool _runtimeHubHostIsHealthy;

    [ObservableProperty]
    private string _runtimeHubHostStatusText = "—";

    [ObservableProperty]
    private bool _hubBroadcastIsHealthy;

    [ObservableProperty]
    private string _hubBroadcastStatusText = "—";

    [ObservableProperty]
    private bool _remoteSnapshotIsHealthy;

    [ObservableProperty]
    private string _remoteSnapshotStatusText = "—";

    [ObservableProperty]
    private bool _runtimeSnapshotIsHealthy;

    [ObservableProperty]
    private string _runtimeSnapshotStatusText = "—";

    [ObservableProperty]
    private bool _restCacheIsHealthy;

    [ObservableProperty]
    private string _restCacheStatusText = "—";

    [ObservableProperty]
    private bool _solutionSyncIsHealthy;

    [ObservableProperty]
    private string _solutionSyncStatusText = "—";

    [ObservableProperty]
    private bool _solutionLoadedIsHealthy;

    [ObservableProperty]
    private string _solutionLoadedStatusText = "—";

    private void OnMobaflowSyncDiagnosticsChanged(MobaflowSyncDiagnosticsChangedEvent e)
    {
        var diagnostics = e.Diagnostics with { HasActiveProject = SelectedProject != null };
        UpdateSyncDiagnostics(diagnostics);
    }

    private void UpdateSyncDiagnostics(MobaflowSyncDiagnostics diagnostics)
    {
        RuntimeHubHostIsHealthy = diagnostics.HostClientConnected && diagnostics.ServerHasHost;
        RuntimeHubHostStatusText = !diagnostics.RestApiReachable
            ? "REST API unreachable"
            : RuntimeHubHostIsHealthy
                ? "Connected"
                : diagnostics.HostClientConnected
                    ? "Host not registered on server"
                    : "Host client disconnected";

        HubBroadcastIsHealthy = diagnostics.LastHubPushSucceeded
            && IsRecent(diagnostics.LastHubPushAt, SyncFreshnessWindow);
        HubBroadcastStatusText = !diagnostics.LastHubPushAt.HasValue
            ? "No snapshot push yet"
            : diagnostics.LastHubPushSucceeded
                ? FormatLastPush(diagnostics.LastHubPushAt)
                : "Last push failed";

        RemoteSnapshotIsHealthy = diagnostics.RemoteClientCount > 0 && diagnostics.SessionOperational;
        RemoteSnapshotStatusText = diagnostics.RemoteClientCount == 0
            ? "No remote clients"
            : diagnostics.SessionOperational
                ? $"{diagnostics.RemoteClientCount} remote client(s), session active"
                : $"{diagnostics.RemoteClientCount} remote client(s), session inactive";

        RuntimeSnapshotIsHealthy = IsRecent(diagnostics.LocalSnapshotCreatedAt, SyncFreshnessWindow)
            || (diagnostics.Z21Connected && diagnostics.HasActiveProject);
        RuntimeSnapshotStatusText = RuntimeSnapshotIsHealthy
            ? $"Updated {FormatAge(diagnostics.LocalSnapshotCreatedAt)}, {diagnostics.LocalSignalBoxElementCount} signals, {diagnostics.LocalLocomotiveFleetCount} locos"
            : "No recent snapshot";

        RestCacheIsHealthy = diagnostics.RestCacheAvailable
            && diagnostics.RestCacheUpdatedAt.HasValue
            && IsRestCacheFresh(diagnostics);
        RestCacheStatusText = !diagnostics.RestCacheAvailable
            ? "No snapshot cached"
            : diagnostics.RestCacheUpdatedAt.HasValue
                ? $"Updated {FormatAge(diagnostics.RestCacheUpdatedAt)}, {diagnostics.RestCacheSignalBoxElementCount} signals, {diagnostics.RestCacheLocomotiveFleetCount} locos"
                : "Cache has no timestamp";

        SolutionSyncIsHealthy = diagnostics.SolutionAvailable && diagnostics.SolutionUpdatedAt.HasValue;
        SolutionSyncStatusText = !diagnostics.SolutionAvailable
            ? "No solution cached"
            : diagnostics.SolutionUpdatedAt.HasValue
                ? $"Updated {FormatAge(diagnostics.SolutionUpdatedAt)}, project {diagnostics.SolutionActiveProjectName ?? "—"}"
                : "Solution cache has no timestamp";

        UpdateSolutionLoadedStatus();
    }

    partial void OnCurrentSolutionPathChanged(string? value) => UpdateSolutionLoadedStatus();

    private void UpdateSolutionLoadedStatus()
    {
        if (string.IsNullOrWhiteSpace(CurrentSolutionPath))
        {
            SolutionLoadedIsHealthy = false;
            SolutionLoadedStatusText = HasSolution
                ? "In-memory only, no solution file loaded"
                : "No solution loaded";
            return;
        }

        SolutionLoadedIsHealthy = true;
        var fileName = Path.GetFileName(CurrentSolutionPath);
        var projectName = SelectedProject?.Name;
        SolutionLoadedStatusText = string.IsNullOrWhiteSpace(projectName)
            ? $"Loaded from {fileName}"
            : $"Loaded from {fileName}, project {projectName}";
    }

    private static bool IsRestCacheFresh(MobaflowSyncDiagnostics diagnostics)
    {
        if (!diagnostics.RestCacheUpdatedAt.HasValue)
        {
            return false;
        }

        var cacheUpdatedAt = diagnostics.RestCacheUpdatedAt.Value.ToUniversalTime();
        var localCreatedAt = diagnostics.LocalSnapshotCreatedAt.ToUniversalTime();
        return cacheUpdatedAt + RestCacheTolerance >= localCreatedAt;
    }

    private static bool IsRecent(DateTimeOffset? timestamp, TimeSpan window)
    {
        if (!timestamp.HasValue)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - timestamp.Value.ToUniversalTime() <= window;
    }

    private static bool IsRecent(DateTimeOffset timestamp, TimeSpan window)
    {
        return DateTimeOffset.UtcNow - timestamp.ToUniversalTime() <= window;
    }

    private static string FormatLastPush(DateTimeOffset? timestamp)
    {
        return $"Last push {FormatAge(timestamp)}";
    }

    private static string FormatAge(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue)
        {
            return "unknown";
        }

        var age = DateTimeOffset.UtcNow - timestamp.Value.ToUniversalTime();
        if (age < TimeSpan.FromSeconds(5))
        {
            return "just now";
        }

        if (age < TimeSpan.FromMinutes(1))
        {
            return $"{(int)age.TotalSeconds} s ago";
        }

        if (age < TimeSpan.FromHours(1))
        {
            return $"{(int)age.TotalMinutes} min ago";
        }

        return $"{(int)age.TotalHours} h ago";
    }
}
