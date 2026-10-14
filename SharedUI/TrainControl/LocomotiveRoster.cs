// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.TrainControl;

using Backend.Interface;

using Common.Runtime;

using Domain;

using Interface;

using System.Collections.ObjectModel;

using ViewModel;

/// <summary>
/// Result of <see cref="LocomotiveRoster.MergeFleet"/>.
/// </summary>
/// <param name="CollectionChanged">Whether locomotives were added, removed or reordered.</param>
/// <param name="MetadataChanged">Whether a locomotive's name, address, photo or function appearance changed.</param>
public sealed record LocomotiveRosterMerge(bool CollectionChanged, bool MetadataChanged);

/// <summary>
/// A locomotive the picker should select again, and whether it replaces a missing saved selection.
/// </summary>
/// <param name="Locomotive">The locomotive to select.</param>
/// <param name="IsFallback">Whether the saved locomotive was missing and the first one was taken instead.</param>
public sealed record LocomotiveRosterSelection(LocomotiveViewModel Locomotive, bool IsFallback);

/// <summary>
/// The locomotives the train-control picker offers: a live fleet from the runtime, otherwise the runtime's own
/// fleet or the selected project's locomotives. Live fleets update the existing list in place, so photo and
/// selection bindings survive; identical fleets and fleets arriving while updates are paused are held back.
/// </summary>
public sealed class LocomotiveRoster
{
    private readonly IRuntimeSnapshotProvider _runtimeSnapshots;
    private readonly IProjectContext? _projectContext;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _pendingFleet;
    private IReadOnlyList<LocomotiveFleetSnapshot>? _lastAppliedFleet;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveRoster"/> class.
    /// </summary>
    /// <param name="runtimeSnapshots">The runtime whose fleet is used when no live fleet arrives.</param>
    /// <param name="projectContext">Optional: the selected project and its locomotives.</param>
    public LocomotiveRoster(IRuntimeSnapshotProvider runtimeSnapshots, IProjectContext? projectContext = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeSnapshots);
        _runtimeSnapshots = runtimeSnapshots;
        _projectContext = projectContext;
    }

    /// <summary>Keeps a non-empty fleet that arrived while updates are paused.</summary>
    public void DeferFleet(IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        if (fleet.Count > 0)
        {
            _pendingFleet = fleet;
        }
    }

    /// <summary>Returns and forgets the fleet held back while paused, or null.</summary>
    public IReadOnlyList<LocomotiveFleetSnapshot>? TakePendingFleet()
    {
        var pending = _pendingFleet;
        _pendingFleet = null;
        return pending;
    }

    /// <summary>
    /// Updates <paramref name="items"/> in place to match a live fleet ordered by name: removes missing, inserts
    /// new and refreshes changed locomotives. Returns null when the fleet equals the last one applied.
    /// </summary>
    public LocomotiveRosterMerge? MergeFleet(
        ObservableCollection<LocomotiveViewModel> items,
        IReadOnlyList<LocomotiveFleetSnapshot> fleet)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(fleet);
        var ordered = OrderByName(fleet);
        if (_lastAppliedFleet is not null && LocomotiveFleetSnapshotComparer.OrderedContentEquals(ordered, _lastAppliedFleet))
        {
            return null;
        }

        var existingById = items.ToDictionary(item => item.Model.Id);
        var snapshotIds = ordered.Select(item => item.LocomotiveId).ToHashSet();
        for (var index = items.Count - 1; index >= 0; index--)
        {
            if (!snapshotIds.Contains(items[index].Model.Id))
            {
                items.RemoveAt(index);
            }
        }

        var collectionChanged = items.Count != ordered.Count;
        var metadataChanged = false;
        for (var index = 0; index < ordered.Count; index++)
        {
            var snapshot = ordered[index];
            if (existingById.TryGetValue(snapshot.LocomotiveId, out var existing))
            {
                if (!existing.MatchesFleetSnapshot(snapshot))
                {
                    existing.ApplyFleetSnapshot(snapshot);
                    metadataChanged = true;
                }

                collectionChanged |= !ReferenceEquals(items[index], existing);
                continue;
            }

            items.Insert(index, CreateFromFleetSnapshot(snapshot));
            collectionChanged = true;
            metadataChanged = true;
        }

        _lastAppliedFleet = ordered;
        return new LocomotiveRosterMerge(collectionChanged, metadataChanged);
    }

    /// <summary>
    /// Builds a new list from the runtime's fleet or, without one, the selected project's locomotives.
    /// </summary>
    public ObservableCollection<LocomotiveViewModel> BuildItems()
    {
        _lastAppliedFleet = null;
        var runtimeFleet = _runtimeSnapshots.Current.LocomotiveFleet;
        IReadOnlyList<LocomotiveViewModel> items = runtimeFleet.Count > 0
            ? OrderByName(runtimeFleet).Select(CreateFromFleetSnapshot).ToList()
            : _projectContext?.SelectedProject?.Locomotives ?? (IReadOnlyList<LocomotiveViewModel>)[];
        return items.Count == 0 ? [] : new ObservableCollection<LocomotiveViewModel>(items);
    }

    /// <summary>
    /// Picks the locomotive to select when none is: the saved one, otherwise the first; null for an empty list.
    /// </summary>
    public static LocomotiveRosterSelection? FindSelectionToRestore(IReadOnlyList<LocomotiveViewModel> items, Guid? savedId)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            return null;
        }

        var saved = savedId.HasValue ? items.FirstOrDefault(item => item.Model.Id == savedId.Value) : null;
        return saved is null
            ? new LocomotiveRosterSelection(items[0], IsFallback: true)
            : new LocomotiveRosterSelection(saved, IsFallback: false);
    }

    /// <summary>Marks the selected locomotive in the picker list.</summary>
    public static void SyncPickerSelection(IEnumerable<LocomotiveViewModel> items, Guid? selectedId)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items)
        {
            item.IsPickerSelected = selectedId.HasValue && item.Model.Id == selectedId.Value;
        }
    }

    private static List<LocomotiveFleetSnapshot> OrderByName(IEnumerable<LocomotiveFleetSnapshot> fleet) =>
        fleet.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    private static LocomotiveViewModel CreateFromFleetSnapshot(LocomotiveFleetSnapshot snapshot) =>
        new(new Locomotive
        {
            Id = snapshot.LocomotiveId,
            Name = snapshot.Name,
            DigitalAddress = snapshot.DigitalAddress,
            PhotoPath = snapshot.PhotoPath,
            FunctionSymbols = snapshot.FunctionSymbols?.ToList(),
            FunctionColors = snapshot.FunctionColors?.ToList(),
            FunctionLabels = snapshot.FunctionLabels?.ToList()
        });
}
