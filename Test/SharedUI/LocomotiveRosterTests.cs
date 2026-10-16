// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Service;
using Moba.SharedUI.TrainControl;
using Moba.SharedUI.ViewModel;

using Moq;

using System.Collections.ObjectModel;

/// <summary>
/// Source choice, in-place fleet merge, pause handling and selection restore of the train-control locomotive list.
/// </summary>
[TestFixture]
internal sealed class LocomotiveRosterTests
{
    private static readonly Guid Br110Id = Guid.Parse("bb15c10a-5b78-451f-8f2f-2d4e3efa74af");
    private static readonly Guid Br211Id = Guid.Parse("f8a91b2c-3d4e-5f60-a211-279170442317");
    private static readonly Guid Br218Id = Guid.Parse("0c9d6f1e-73b2-4c55-9a41-5f0d2e1b7a90");
    private static readonly string[] OrderedNames = ["BR 110", "BR 211"];
    private static readonly string[] MergedNames = ["BR 110", "BR 218"];
    private static readonly bool[] FirstSelected = [true, false];

    private Mock<IRuntimeSnapshotProvider> _runtime = null!;
    private MobileSolutionContext _projectContext = null!;
    private LocomotiveRoster _roster = null!;

    [SetUp]
    public void SetUp()
    {
        _runtime = new Mock<IRuntimeSnapshotProvider>();
        _runtime.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        _projectContext = new MobileSolutionContext();
        _roster = new LocomotiveRoster(_runtime.Object, _projectContext);
    }

    [Test]
    public void MergeFleet_UpdatesTheListInPlaceAndKeepsExistingItems()
    {
        var items = new ObservableCollection<LocomotiveViewModel>();
        _roster.MergeFleet(items, [Fleet(Br211Id, "BR 211"), Fleet(Br110Id, "BR 110")]);
        var br110 = items[0];

        var merge = _roster.MergeFleet(items, [Fleet(Br110Id, "BR 110", "new.jpg"), Fleet(Br218Id, "BR 218")]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(merge, Is.EqualTo(new LocomotiveRosterMerge(CollectionChanged: true, MetadataChanged: true)));
            Assert.That(items.Select(item => item.Name), Is.EqualTo(MergedNames));
            Assert.That(items[0], Is.SameAs(br110));
            Assert.That(items[0].Model.PhotoPath, Is.EqualTo("new.jpg"));
        }
    }

    [Test]
    public void MergeFleet_IgnoresAFleetEqualToTheLastOneUntilTheListIsRebuilt()
    {
        var items = new ObservableCollection<LocomotiveViewModel>();
        IReadOnlyList<LocomotiveFleetSnapshot> fleet = [Fleet(Br110Id, "BR 110")];
        _roster.MergeFleet(items, fleet);

        var repeated = _roster.MergeFleet(items, fleet);
        _roster.BuildItems();
        var afterRebuild = _roster.MergeFleet(items, fleet);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(repeated, Is.Null);
            Assert.That(afterRebuild, Is.EqualTo(new LocomotiveRosterMerge(CollectionChanged: false, MetadataChanged: false)));
        }
    }

    [Test]
    public void BuildItems_PrefersTheRuntimeFleetOverTheProject()
    {
        ApplyProject(new Locomotive { Id = Br218Id, Name = "BR 218" });
        _runtime.SetupGet(runtime => runtime.Current).Returns(new MobaRuntimeSnapshot
        {
            LocomotiveFleet = [Fleet(Br211Id, "BR 211"), Fleet(Br110Id, "BR 110")],
        });

        var items = _roster.BuildItems();

        Assert.That(items.Select(item => item.Name), Is.EqualTo(OrderedNames));
    }

    [Test]
    public void BuildItems_UsesTheProjectLocomotivesWithoutARuntimeFleet()
    {
        var withoutProject = _roster.BuildItems();
        ApplyProject(new Locomotive { Id = Br218Id, Name = "BR 218" });

        var withProject = _roster.BuildItems();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutProject, Is.Empty);
            Assert.That(withProject.Single().Model.Id, Is.EqualTo(Br218Id));
        }
    }

    [Test]
    public void DeferFleet_KeepsTheLastNonEmptyFleetUntilItIsTaken()
    {
        IReadOnlyList<LocomotiveFleetSnapshot> fleet = [Fleet(Br110Id, "BR 110")];

        _roster.DeferFleet(fleet);
        _roster.DeferFleet([]);
        var taken = _roster.TakePendingFleet();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(taken, Is.SameAs(fleet));
            Assert.That(_roster.TakePendingFleet(), Is.Null);
        }
    }

    [Test]
    public void FindSelectionToRestore_PrefersTheSavedLocomotiveAndFallsBackToTheFirst()
    {
        var items = new List<LocomotiveViewModel>
        {
            new(new Locomotive { Id = Br110Id, Name = "BR 110" }),
            new(new Locomotive { Id = Br211Id, Name = "BR 211" }),
        };

        var saved = LocomotiveRoster.FindSelectionToRestore(items, Br211Id);
        var missing = LocomotiveRoster.FindSelectionToRestore(items, Br218Id);
        var none = LocomotiveRoster.FindSelectionToRestore(items, savedId: null);
        var empty = LocomotiveRoster.FindSelectionToRestore([], Br110Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(saved, Is.EqualTo(new LocomotiveRosterSelection(items[1], IsFallback: false)));
            Assert.That(missing, Is.EqualTo(new LocomotiveRosterSelection(items[0], IsFallback: true)));
            Assert.That(none, Is.EqualTo(new LocomotiveRosterSelection(items[0], IsFallback: true)));
            Assert.That(empty, Is.Null);
        }
    }

    [Test]
    public void SyncPickerSelection_MarksOnlyTheSelectedLocomotive()
    {
        var items = new List<LocomotiveViewModel>
        {
            new(new Locomotive { Id = Br110Id, Name = "BR 110" }),
            new(new Locomotive { Id = Br211Id, Name = "BR 211" }) { IsPickerSelected = true },
        };

        LocomotiveRoster.SyncPickerSelection(items, Br110Id);

        Assert.That(items.Select(item => item.IsPickerSelected), Is.EqualTo(FirstSelected));
    }

    private void ApplyProject(params Locomotive[] locomotives) =>
        _projectContext.ApplySolution(
            new Solution { Projects = [new Project { Name = "myMOBA", Locomotives = [.. locomotives] }] },
            "myMOBA");

    private static LocomotiveFleetSnapshot Fleet(Guid id, string name, string? photoPath = null) =>
        new() { LocomotiveId = id, Name = name, PhotoPath = photoPath };
}
