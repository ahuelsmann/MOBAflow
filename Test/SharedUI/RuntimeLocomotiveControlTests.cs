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

    private static Mock<IProjectRuntimeSnapshots> Snapshots(params MobaRuntimeSnapshot[] current)
    {
        var snapshots = new Mock<IProjectRuntimeSnapshots>();
        snapshots.SetupGet(value => value.Snapshots).Returns(current);
        return snapshots;
    }

    private static void Publish(Mock<IProjectRuntimeSnapshots> snapshots, MobaRuntimeSnapshot snapshot) =>
        snapshots.Raise(value => value.RuntimeSnapshotChanged += null, new ProjectRuntimeSnapshotEventArgs(snapshot));

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
