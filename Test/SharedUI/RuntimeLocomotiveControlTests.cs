// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Backend.Interface;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

using Moq;

/// <summary>
/// Which locomotives a project's runtime controls, from its snapshots.
/// </summary>
[TestFixture]
internal sealed class RuntimeLocomotiveControlTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();

    [Test]
    public void ExistingSnapshots_AreKnownFromTheStart()
    {
        var snapshots = Snapshots(Snapshot(isConnected: true, (5, 0)));
        using var control = new RuntimeLocomotiveControl(snapshots.Object, Dispatcher());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(control.IsControlled(ProjectId, new Locomotive { DigitalAddress = 5 }), Is.True);
            Assert.That(control.IsControlled(ProjectId, new Locomotive { DigitalAddress = 6 }), Is.False);
        }
    }

    [Test]
    public void SpeedChanges_DoNotRaiseChanged_ButANewLocomotiveDoes()
    {
        var snapshots = Snapshots(Snapshot(isConnected: true, (5, 0)));
        using var control = new RuntimeLocomotiveControl(snapshots.Object, Dispatcher());
        var changes = 0;
        control.Changed += (_, _) => changes++;

        Publish(snapshots, Snapshot(isConnected: true, (5, 60)));
        var afterSpeedChange = changes;
        Publish(snapshots, Snapshot(isConnected: true, (5, 60), (7, 0)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterSpeedChange, Is.Zero);
            Assert.That(changes, Is.EqualTo(1));
        }
    }

    [Test]
    public void Dispose_StopsListeningToSnapshots()
    {
        var snapshots = Snapshots();
        var control = new RuntimeLocomotiveControl(snapshots.Object, Dispatcher());

        control.Dispose();

        snapshots.VerifyRemove(value => value.RuntimeSnapshotChanged -= It.IsAny<EventHandler<ProjectRuntimeSnapshotEventArgs>>(), Times.Once);
    }

    [Test]
    public void LateEventOfAnOlderSnapshot_DoesNotReplaceTheCurrentState()
    {
        var older = Snapshot(isConnected: true, (5, 0));
        var snapshots = Snapshots(Snapshot(isConnected: true, (5, 0), (7, 0)));
        using var control = new RuntimeLocomotiveControl(snapshots.Object, Dispatcher());

        // Published concurrently, the older snapshot's event arrives after the newer snapshot became current.
        snapshots.Raise(value => value.RuntimeSnapshotChanged += null, new ProjectRuntimeSnapshotEventArgs(older));

        Assert.That(control.IsControlled(ProjectId, new Locomotive { DigitalAddress = 7 }), Is.True);
    }

    [Test]
    public void SnapshotPublishedWhileTheCurrentOnesAreRead_IsNotLost()
    {
        MobaRuntimeSnapshot[] current = [Snapshot(isConnected: true, (5, 0))];
        var snapshots = new Mock<IProjectRuntimeSnapshots>();
        var published = false;
        snapshots.SetupGet(value => value.Snapshots).Returns(() =>
        {
            var read = current;
            if (!published)
            {
                // The runtime publishes a newer snapshot while the control reads the current ones.
                published = true;
                current = [Snapshot(isConnected: true, (5, 0), (7, 0))];
                snapshots.Raise(value => value.RuntimeSnapshotChanged += null, new ProjectRuntimeSnapshotEventArgs(current[0]));
            }

            return read;
        });

        using var control = new RuntimeLocomotiveControl(snapshots.Object, Dispatcher());

        Assert.That(control.IsControlled(ProjectId, new Locomotive { DigitalAddress = 7 }), Is.True);
    }

    private static Mock<IProjectRuntimeSnapshots> Snapshots(params MobaRuntimeSnapshot[] current)
    {
        var snapshots = new Mock<IProjectRuntimeSnapshots>();
        snapshots.SetupGet(value => value.Snapshots).Returns(current);
        return snapshots;
    }

    // The runtime replaces its current snapshot, then publishes it.
    private static void Publish(Mock<IProjectRuntimeSnapshots> snapshots, MobaRuntimeSnapshot snapshot)
    {
        snapshots.SetupGet(value => value.Snapshots).Returns(new[] { snapshot });
        snapshots.Raise(value => value.RuntimeSnapshotChanged += null, new ProjectRuntimeSnapshotEventArgs(snapshot));
    }

    private static MobaRuntimeSnapshot Snapshot(bool isConnected, params (int Address, int Speed)[] locomotives) => new()
    {
        ProjectId = ProjectId,
        IsConnected = isConnected,
        LocomotiveStates = locomotives.ToDictionary(
            locomotive => locomotive.Address,
            locomotive => new LocomotiveRuntimeSnapshot { Address = locomotive.Address, Speed = locomotive.Speed })
    };

    private static IUiDispatcher Dispatcher()
    {
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        return dispatcher.Object;
    }
}
