// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;
using Moba.SharedUI.Service;

using Moq;

/// <summary>
/// Source selection and project filters for the locomotive fleet and signal-box data MOBAsmart shows.
/// </summary>
[TestFixture]
internal sealed class MobileRuntimeProjectionTests
{
    private static readonly Guid Br110Id = Guid.Parse("bb15c10a-5b78-451f-8f2f-2d4e3efa74af");
    private static readonly Guid Br211Id = Guid.Parse("f8a91b2c-3d4e-5f60-a211-279170442317");
    private static readonly Guid OtherProjectLocoId = Guid.Parse("0c9d6f1e-73b2-4c55-9a41-5f0d2e1b7a90");
    private static readonly string[] RemotePhoto = ["remote/110.jpg"];
    private static readonly string[] ProjectNames = ["BR 110", "BR 211"];

    private Mock<IRuntimeSnapshotProvider> _runtime = null!;
    private Mock<IMobileRuntimeCoordinator> _coordinator = null!;
    private Mock<IMobileSolutionStore> _store = null!;
    private MobileSolutionContext _projectContext = null!;
    private MobileRuntimeProjection _projection = null!;

    [SetUp]
    public void SetUp()
    {
        _runtime = new Mock<IRuntimeSnapshotProvider>();
        _runtime.SetupGet(runtime => runtime.Current).Returns(MobaRuntimeSnapshot.Empty);
        _coordinator = new Mock<IMobileRuntimeCoordinator>();
        _store = new Mock<IMobileSolutionStore>();
        _projectContext = new MobileSolutionContext();
        _projectContext.ApplySolution(
            new Solution
            {
                Projects =
                [
                    new Project
                    {
                        Name = "myMOBA",
                        Locomotives =
                        [
                            new Locomotive { Id = Br110Id, Name = "BR 110", DigitalAddress = 110, PhotoPath = "project/110.jpg" },
                            new Locomotive { Id = Br211Id, Name = "BR 211", DigitalAddress = 211 },
                        ],
                    },
                ],
            },
            "myMOBA");
        _projection = new MobileRuntimeProjection(_runtime.Object, _projectContext, _coordinator.Object, _store.Object);
    }

    [Test]
    public async Task GetBestAvailableLocomotiveFleet_PrefersTheLiveRemoteFleetWhileTheRemoteRuntimeIsPreferred()
    {
        _coordinator.SetupGet(coordinator => coordinator.PreferRemoteRuntime).Returns(true);
        await _projection.CacheRemoteLocomotiveFleet([Fleet(Br110Id, "BR 110", "remote/110.jpg")], CancellationToken.None)
            .ConfigureAwait(false);

        var fleet = _projection.GetBestAvailableLocomotiveFleet();

        Assert.That(fleet.Select(item => item.PhotoPath), Is.EqualTo(RemotePhoto));
    }

    [Test]
    public void GetBestAvailableLocomotiveFleet_PrefersTheProjectOverARestoredCache()
    {
        _coordinator.SetupGet(coordinator => coordinator.PreferRemoteRuntime).Returns(true);
        _projection.RestoreCachedLocomotiveFleet([Fleet(Br110Id, "BR 110", "stale/110.jpg")]);

        var fleet = _projection.GetBestAvailableLocomotiveFleet();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fleet.Select(item => item.Name), Is.EqualTo(ProjectNames));
            Assert.That(fleet[0].PhotoPath, Is.EqualTo("project/110.jpg"));
        }
    }

    [Test]
    public void ResolveAuthoritativeLocomotiveFleet_CompletesTheProjectFleetWithRemoteData()
    {
        var fleet = _projection.ResolveAuthoritativeLocomotiveFleet(
        [
            Fleet(Br211Id, "BR 211", "remote/211.jpg"),
            Fleet(OtherProjectLocoId, "Other", null),
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fleet.Select(item => item.LocomotiveId), Is.EqualTo(new[] { Br110Id, Br211Id }));
            Assert.That(fleet[1].PhotoPath, Is.EqualTo("remote/211.jpg"));
        }
    }

    [Test]
    public void ResolveAuthoritativeLocomotiveFleet_IgnoresAFleetOfAnotherProject()
    {
        var fleet = _projection.ResolveAuthoritativeLocomotiveFleet([Fleet(OtherProjectLocoId, "Other", null)]);

        Assert.That(fleet, Is.Empty);
    }

    [Test]
    public void ShouldCacheRemoteLocomotiveFleet_OnlyWhenItCoversTheProject()
    {
        var partial = _projection.ShouldCacheRemoteLocomotiveFleet([Fleet(Br110Id, "BR 110", null)]);
        var complete = _projection.ShouldCacheRemoteLocomotiveFleet(
            [Fleet(Br110Id, "BR 110", null), Fleet(Br211Id, "BR 211", null)]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(partial, Is.False);
            Assert.That(complete, Is.True);
        }
    }

    [Test]
    public async Task CacheRemoteData_PersistsForOfflineUseAndIgnoresEmptyLists()
    {
        IReadOnlyList<LocomotiveFleetSnapshot> fleet = [Fleet(Br110Id, "BR 110", null)];
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements = [new() { ElementId = Guid.NewGuid(), Name = "A1" }];

        await _projection.CacheRemoteLocomotiveFleet(fleet, CancellationToken.None).ConfigureAwait(false);
        await _projection.CacheRemoteSignalBoxElements(elements, CancellationToken.None).ConfigureAwait(false);
        await _projection.CacheRemoteLocomotiveFleet([], CancellationToken.None).ConfigureAwait(false);
        await _projection.CacheRemoteSignalBoxElements([], CancellationToken.None).ConfigureAwait(false);

        _store.Verify(store => store.SaveLocomotiveFleetAsync(fleet, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(store => store.SaveSignalBoxElementsAsync(elements, It.IsAny<CancellationToken>()), Times.Once);
        _store.VerifyNoOtherCalls();
    }

    [Test]
    public void SignalBoxElements_UseTheCacheWithoutAPlanUntilItIsCleared()
    {
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> cached = [new() { ElementId = Guid.NewGuid(), Name = "A1" }];
        _projection.RestoreCachedSignalBoxElements(cached);

        var restored = _projection.GetBestAvailableSignalBoxElements();
        var availableBeforeClear = _projection.HasAnySignalBoxElementsAvailable;
        _projection.ClearRemoteSignalBoxElements();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored, Is.SameAs(cached));
            Assert.That(availableBeforeClear, Is.True);
            Assert.That(_projection.HasAnySignalBoxElementsAvailable, Is.False);
            Assert.That(_projection.GetBestAvailableSignalBoxElements(), Is.Empty);
        }
    }

    private static LocomotiveFleetSnapshot Fleet(Guid id, string name, string? photoPath) =>
        new() { LocomotiveId = id, Name = name, PhotoPath = photoPath };
}
