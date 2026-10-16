// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Mobile;

using Backend.Interface;

using Contracts.Runtime;

using Domain;

using Interface;

using Service;

/// <summary>
/// The locomotive fleet and signal-box elements MOBAsmart shows for the selected project. It keeps the last
/// remote data from MOBAflow (also restored from the phone's cache) and decides which source is best: live
/// remote data, the synced project, the cache or the local runtime. It returns data; tab visibility and
/// publishing stay with the root ViewModel.
/// </summary>
public sealed class MobileRuntimeProjection
{
    private readonly IRuntimeSnapshotProvider _runtimeSnapshots;
    private readonly IProjectContext? _projectContext;
    private readonly IMobileRuntimeCoordinator? _mobileRuntimeCoordinator;
    private readonly IMobileSolutionStore? _mobileSolutionStore;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _cachedRemoteLocomotiveFleet;
    private IReadOnlyList<SignalBoxElementRuntimeSnapshot>? _cachedRemoteSignalBoxElements;
    private bool _remoteLocomotiveFleetIsLive;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileRuntimeProjection"/> class.
    /// </summary>
    /// <param name="runtimeSnapshots">The phone's local runtime snapshots.</param>
    /// <param name="projectContext">Optional: the synced solution and the selected project.</param>
    /// <param name="mobileRuntimeCoordinator">Optional: tells whether the remote MOBAflow runtime is preferred.</param>
    /// <param name="mobileSolutionStore">Optional: persists remote data for offline use.</param>
    public MobileRuntimeProjection(
        IRuntimeSnapshotProvider runtimeSnapshots,
        IProjectContext? projectContext = null,
        IMobileRuntimeCoordinator? mobileRuntimeCoordinator = null,
        IMobileSolutionStore? mobileSolutionStore = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        _runtimeSnapshots = runtimeSnapshots;
        _projectContext = projectContext;
        _mobileRuntimeCoordinator = mobileRuntimeCoordinator;
        _mobileSolutionStore = mobileSolutionStore;
    }

    private bool PreferRemoteRuntime => _mobileRuntimeCoordinator?.PreferRemoteRuntime == true;

    /// <summary>Gets whether any source has locomotives for the Control tab.</summary>
    public bool HasAnyLocomotiveFleetAvailable =>
        _cachedRemoteLocomotiveFleet is { Count: > 0 }
        || _runtimeSnapshots.Current.LocomotiveFleet.Count > 0
        || (_projectContext?.SelectedProject?.Locomotives.Count ?? 0) > 0;

    /// <summary>Gets whether remote or local signal-box elements exist.</summary>
    public bool HasAnySignalBoxElementsAvailable =>
        _cachedRemoteSignalBoxElements is { Count: > 0 }
        || _runtimeSnapshots.Current.SignalBoxElements.Count > 0;

    /// <summary>
    /// Restores the signal-box elements and locomotive fleet of the cached active project at app startup.
    /// </summary>
    public void RestoreCachedMobileSnapshot(MobileSolutionCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var activeProject = ResolveCachedActiveProject(entry);
        RestoreCachedSignalBoxElements(SignalBoxRuntimeSync.FilterToPlan(activeProject.SignalBoxPlan, entry.SignalBoxElements));
        RestoreCachedLocomotiveFleet(FilterFleetToProject(activeProject, entry.LocomotiveFleet));
    }

    /// <summary>Restores a cached locomotive fleet; it counts as stale until MOBAflow sends a live one.</summary>
    public void RestoreCachedLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        if (fleet.Count == 0)
        {
            return;
        }

        _cachedRemoteLocomotiveFleet = fleet;
        _remoteLocomotiveFleetIsLive = false;
    }

    /// <summary>Restores cached signal-box elements.</summary>
    public void RestoreCachedSignalBoxElements(IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (elements.Count == 0)
        {
            return;
        }

        _cachedRemoteSignalBoxElements = elements;
    }

    /// <summary>
    /// Keeps a live locomotive fleet and returns the task that persists it for offline use.
    /// </summary>
    public Task CacheRemoteLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        if (fleet.Count == 0)
        {
            return Task.CompletedTask;
        }

        _cachedRemoteLocomotiveFleet = fleet;
        _remoteLocomotiveFleetIsLive = true;
        return _mobileSolutionStore?.SaveLocomotiveFleetAsync(fleet, cancellationToken) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Keeps remote signal-box elements and returns the task that persists them for offline use.
    /// </summary>
    public Task CacheRemoteSignalBoxElements(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (elements.Count == 0)
        {
            return Task.CompletedTask;
        }

        _cachedRemoteSignalBoxElements = elements;
        return _mobileSolutionStore?.SaveSignalBoxElementsAsync(elements, cancellationToken) ?? Task.CompletedTask;
    }

    /// <summary>Forgets the remote locomotive fleet.</summary>
    public void ClearRemoteLocomotiveFleet()
    {
        _cachedRemoteLocomotiveFleet = null;
        _remoteLocomotiveFleetIsLive = false;
    }

    /// <summary>Forgets the remote signal-box elements.</summary>
    public void ClearRemoteSignalBoxElements() => _cachedRemoteSignalBoxElements = null;

    /// <summary>
    /// Returns the fleet of the selected project: live remote data while the remote runtime is preferred,
    /// otherwise the synced project, the cache and finally the local runtime.
    /// </summary>
    public IReadOnlyList<LocomotiveFleetSnapshot> GetBestAvailableLocomotiveFleet()
    {
        if (PreferRemoteRuntime)
        {
            if (_remoteLocomotiveFleetIsLive && _cachedRemoteLocomotiveFleet is { Count: > 0 })
            {
                return FilterFleetToCurrentProject(_cachedRemoteLocomotiveFleet);
            }

            var liveRuntimeFleet = FilterFleetToCurrentProject(_runtimeSnapshots.Current.LocomotiveFleet);
            if (liveRuntimeFleet.Count > 0)
            {
                return liveRuntimeFleet;
            }
        }

        var projectFleet = BuildFleetSnapshotsFromProjectContext();
        if (projectFleet.Count > 0)
        {
            return projectFleet;
        }

        if (_cachedRemoteLocomotiveFleet is { Count: > 0 })
        {
            return FilterFleetToCurrentProject(_cachedRemoteLocomotiveFleet);
        }

        return FilterFleetToCurrentProject(_runtimeSnapshots.Current.LocomotiveFleet);
    }

    /// <summary>
    /// Limits a live remote fleet to the selected project and completes it with project locomotives MOBAflow did
    /// not send; returns an empty list when none belongs to the project.
    /// </summary>
    public IReadOnlyList<LocomotiveFleetSnapshot> ResolveAuthoritativeLocomotiveFleet(
        IReadOnlyList<LocomotiveFleetSnapshot> remoteFleet)
    {
        ArgumentNullException.ThrowIfNull(remoteFleet);
        var filtered = FilterFleetToCurrentProject(remoteFleet);
        if (filtered.Count == 0)
        {
            return [];
        }

        var projectFleet = BuildFleetSnapshotsFromProjectContext();
        if (projectFleet.Count == 0 || CoversProject(filtered, projectFleet, item => item.LocomotiveId))
        {
            return OrderByName(filtered);
        }

        var remoteById = filtered.ToDictionary(item => item.LocomotiveId);
        return projectFleet
            .Select(item => remoteById.TryGetValue(item.LocomotiveId, out var remote) ? remote : item)
            .ToList();
    }

    /// <summary>
    /// Whether a remote fleet covers every locomotive of the selected project, so it may replace the cache.
    /// </summary>
    public bool ShouldCacheRemoteLocomotiveFleet(IReadOnlyList<LocomotiveFleetSnapshot> remoteFleet)
    {
        ArgumentNullException.ThrowIfNull(remoteFleet);
        var projectFleet = BuildFleetSnapshotsFromProjectContext();
        return projectFleet.Count == 0 || CoversProject(remoteFleet, projectFleet, item => item.LocomotiveId);
    }

    /// <summary>
    /// Returns the signal-box elements of the selected project's plan with the newest known aspects, or the
    /// cached and local runtime elements without a synced plan.
    /// </summary>
    public IReadOnlyList<SignalBoxElementRuntimeSnapshot> GetBestAvailableSignalBoxElements()
    {
        var projectElements = BuildSignalBoxSnapshotsFromProjectContext();
        if (projectElements.Count > 0)
        {
            return SignalBoxSnapshotMerge.MergeAspectsFromCache(projectElements, GetRuntimeSignalBoxAspectSource());
        }

        var local = _runtimeSnapshots.Current.SignalBoxElements;
        if (_cachedRemoteSignalBoxElements is { Count: > 0 } cached)
        {
            return local.Count > 0 ? SignalBoxSnapshotMerge.MergeAspectsFromCache(local, cached) : cached;
        }

        return local;
    }

    /// <summary>Applies the cached remote aspects to the elements of a local runtime snapshot.</summary>
    public IReadOnlyList<SignalBoxElementRuntimeSnapshot> MergeCachedAspects(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> localElements)
    {
        ArgumentNullException.ThrowIfNull(localElements);
        return _cachedRemoteSignalBoxElements is { Count: > 0 } cached
            ? SignalBoxSnapshotMerge.MergeAspectsFromCache(localElements, cached)
            : localElements;
    }

    /// <summary>Limits remote signal-box elements to the selected project's plan.</summary>
    public IReadOnlyList<SignalBoxElementRuntimeSnapshot> FilterSignalBoxElementsToPlan(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements) =>
        SignalBoxRuntimeSync.FilterToPlan(_projectContext?.SelectedProject?.Model.SignalBoxPlan, elements);

    /// <summary>
    /// Whether remote signal-box elements cover every element of the selected project's plan.
    /// </summary>
    public bool ShouldCacheRemoteSignalBoxElements(IReadOnlyList<SignalBoxElementRuntimeSnapshot> remoteElements)
    {
        ArgumentNullException.ThrowIfNull(remoteElements);
        var projectElements = BuildSignalBoxSnapshotsFromProjectContext();
        return projectElements.Count == 0 || CoversProject(remoteElements, projectElements, element => element.ElementId);
    }

    private IReadOnlyList<SignalBoxElementRuntimeSnapshot> GetRuntimeSignalBoxAspectSource()
    {
        if (PreferRemoteRuntime && _cachedRemoteSignalBoxElements is { Count: > 0 } cached)
        {
            return cached;
        }

        var localElements = _runtimeSnapshots.Current.SignalBoxElements;
        return localElements.Count > 0 ? localElements : _cachedRemoteSignalBoxElements ?? [];
    }

    private IReadOnlyList<SignalBoxElementRuntimeSnapshot> BuildSignalBoxSnapshotsFromProjectContext() =>
        SignalBoxRuntimeSync.BuildSnapshotsFromPlan(_projectContext?.SelectedProject?.Model.SignalBoxPlan);

    private IReadOnlyList<LocomotiveFleetSnapshot> FilterFleetToCurrentProject(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        var project = _projectContext?.SelectedProject?.Model;
        return project == null ? fleet : FilterFleetToProject(project, fleet);
    }

    private List<LocomotiveFleetSnapshot> BuildFleetSnapshotsFromProjectContext()
    {
        var project = _projectContext?.SelectedProject;
        if (project == null)
        {
            return [];
        }

        return OrderByName(project.Locomotives
            .Select(loco => new LocomotiveFleetSnapshot
            {
                LocomotiveId = loco.Model.Id,
                Name = loco.Name,
                DigitalAddress = loco.DigitalAddress,
                PhotoPath = loco.PhotoPath
            }));
    }

    private static List<LocomotiveFleetSnapshot> OrderByName(IEnumerable<LocomotiveFleetSnapshot> fleet) =>
        fleet.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    private static bool CoversProject<T>(IReadOnlyList<T> remote, IReadOnlyList<T> project, Func<T, Guid> id)
    {
        var projectIds = project.Select(id).ToHashSet();
        return remote.Count >= project.Count && remote.All(item => projectIds.Contains(id(item)));
    }

    private static IReadOnlyList<LocomotiveFleetSnapshot> FilterFleetToProject(
        Project project,
        IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        if (fleet.Count == 0)
        {
            return fleet;
        }

        var projectIds = project.Locomotives.Select(loco => loco.Id).ToHashSet();
        return fleet.Where(item => projectIds.Contains(item.LocomotiveId)).ToList();
    }

    private static Project ResolveCachedActiveProject(MobileSolutionCacheEntry entry)
    {
        var activeProjectName = entry.Meta.ActiveProjectName;
        if (!string.IsNullOrWhiteSpace(activeProjectName))
        {
            var match = entry.Solution.Projects.FirstOrDefault(project =>
                string.Equals(project.Name, activeProjectName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }
        }

        return entry.Solution.Projects[0];
    }
}
