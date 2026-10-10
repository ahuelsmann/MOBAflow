#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.SharedUI.Interface;
using Moba.Test.Helpers;
using Moba.WinUI.Service;

using Moq;

/// <summary>
/// Remote commands name their project: each one reaches only the Z21 of that project's runtime.
/// </summary>
[TestFixture]
internal sealed class ProjectRuntimeCommandRouterTests
{
    private TestProjectRuntimeHost _projectRuntimes = null!;
    private ProjectRuntimeCommandRouter _router = null!;

    [SetUp]
    public void SetUp()
    {
        _projectRuntimes = new TestProjectRuntimeHost();
        _router = new ProjectRuntimeCommandRouter(_projectRuntimes.Host, new Mock<IRecordingSessionService>().Object);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _projectRuntimes.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ForProject_SendsTheCommandToThatProjectsZ21Only()
    {
        var station = TestProjectRuntimeHost.Project("Station", "192.168.0.111");
        var yard = TestProjectRuntimeHost.Project("Yard", "192.168.0.112");
        await _projectRuntimes.Host.LoadAsync([station, yard]).ConfigureAwait(false);
        _projectRuntimes.Host.SelectProject(station.Id);

        await _router.ForProject(yard.Id)!.SetLocomotiveDriveAsync(3, 40, forward: true).ConfigureAwait(false);

        _projectRuntimes.Z21s[1].Verify(
            z21 => z21.SetLocoDriveAsync(3, 40, true, It.IsAny<CancellationToken>()),
            Times.Once);
        _projectRuntimes.Z21s[0].Verify(
            z21 => z21.SetLocoDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ForProject_WithoutRuntime_ReturnsNull()
    {
        await _projectRuntimes.Host.LoadAsync([TestProjectRuntimeHost.Project("Station", "192.168.0.111")]).ConfigureAwait(false);

        Assert.That(_router.ForProject(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task Snapshots_NameEveryProject()
    {
        var station = TestProjectRuntimeHost.Project("Station", "192.168.0.111");
        var yard = TestProjectRuntimeHost.Project("Yard", "192.168.0.112");
        await _projectRuntimes.Host.LoadAsync([station, yard]).ConfigureAwait(false);

        Assert.That(_router.Snapshots.Select(snapshot => snapshot.ProjectId), Is.EquivalentTo(new[] { station.Id, yard.Id }));
    }
}
#endif
