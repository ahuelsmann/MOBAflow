#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Test.Helpers;
using Moba.WinUI.Service;

using Moq;

/// <summary>
/// Remote commands name their project: each one reaches only the Z21 of that project's runtime.
/// </summary>
[TestFixture]
internal sealed class ProjectRuntimeCommandRouterTests
{

    [Test]
    public async Task ForProject_SendsTheCommandToThatProjectsZ21Only()
    {
        var projectRuntimes = new TestProjectRuntimeHost();
        await using var lifetime = projectRuntimes.ConfigureAwait(false);
        var router = Router(projectRuntimes);
        var station = TestProjectRuntimeHost.Project("Station", "192.168.0.111");
        var yard = TestProjectRuntimeHost.Project("Yard", "192.168.0.112");
        await projectRuntimes.Host.LoadAsync([station, yard]).ConfigureAwait(false);
        projectRuntimes.Host.SelectProject(station.Id);

        await router.ForProject(yard.Id)!.SetLocomotiveDriveAsync(3, 40, forward: true).ConfigureAwait(false);

        projectRuntimes.Z21s[1].Verify(
            z21 => z21.SetLocoDriveAsync(3, 40, true, It.IsAny<CancellationToken>()),
            Times.Once);
        projectRuntimes.Z21s[0].Verify(
            z21 => z21.SetLocoDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ForProject_WithoutRuntime_ReturnsNull()
    {
        var projectRuntimes = new TestProjectRuntimeHost();
        await using var lifetime = projectRuntimes.ConfigureAwait(false);
        var router = Router(projectRuntimes);
        await projectRuntimes.Host.LoadAsync([TestProjectRuntimeHost.Project("Station", "192.168.0.111")]).ConfigureAwait(false);

        Assert.That(router.ForProject(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task Snapshots_NameEveryProject()
    {
        var projectRuntimes = new TestProjectRuntimeHost();
        await using var lifetime = projectRuntimes.ConfigureAwait(false);
        var router = Router(projectRuntimes);
        var station = TestProjectRuntimeHost.Project("Station", "192.168.0.111");
        var yard = TestProjectRuntimeHost.Project("Yard", "192.168.0.112");
        await projectRuntimes.Host.LoadAsync([station, yard]).ConfigureAwait(false);

        Assert.That(router.Snapshots.Select(snapshot => snapshot.ProjectId), Is.EquivalentTo(new[] { station.Id, yard.Id }));
    }

    private static ProjectRuntimeCommandRouter Router(TestProjectRuntimeHost projectRuntimes) =>
        new(projectRuntimes.Host, new Mock<IRecordingSessionService>().Object);
}
#endif
