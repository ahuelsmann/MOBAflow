// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Service;
using Moba.Backend.Service.Validation;
using Moba.Domain;

using System.Net;

/// <summary>
/// Covers how a project's Z21 assignment is reported and resolved: every project has its own Z21, and with a
/// shared Z21 the earlier project connects.
/// </summary>
[TestFixture]
internal sealed class Z21AssignmentDiagnosticsTests
{
    [Test]
    public void ProjectsWithoutZ21_AreNotAConflict()
    {
        var station = Project("Station", string.Empty);
        var yard = Project("Yard", string.Empty);

        Assert.That(Z21AssignmentDiagnostics.Analyze(yard, [station, yard]), Is.Empty);
    }

    [Test]
    public void SharedZ21_IsAnErrorForTheLaterAndAWarningForTheEarlierProject()
    {
        var station = Project("Station", "192.168.0.111");
        var yard = Project("Yard", " 192.168.0.111 ");
        Project[] solution = [station, yard];

        var later = Z21AssignmentDiagnostics.Analyze(yard, solution).Single();
        var earlier = Z21AssignmentDiagnostics.Analyze(station, solution).Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(later.Severity, Is.EqualTo(ProjectDiagnosticSeverity.Error));
            Assert.That(later.Message, Does.Contain("already used by project 'Station'"));
            Assert.That(earlier.Severity, Is.EqualTo(ProjectDiagnosticSeverity.Warning));
            Assert.That(earlier.Message, Does.Contain("'Yard'"));
        }
    }

    [Test]
    public void OwnZ21_ReportsNothing()
    {
        var station = Project("Station", "192.168.0.111");
        var yard = Project("Yard", "192.168.0.112");

        Assert.That(Z21AssignmentDiagnostics.Analyze(station, [station, yard]), Is.Empty);
    }

    [Test]
    public async Task ProjectEndpointSource_ResolvesTheAssignedZ21AndNeverSearches()
    {
        var resolved = await new ProjectZ21EndpointSource(Project("Station", "192.168.0.111", port: 21106))
            .ResolveAsync(rediscover: true).ConfigureAwait(false);
        var missing = await new ProjectZ21EndpointSource(Project("Yard", string.Empty))
            .ResolveAsync(rediscover: true).ConfigureAwait(false);
        var blocked = await new ProjectZ21EndpointSource(Project("Yard", "192.168.0.111"), "Z21 is used by 'Station'")
            .ResolveAsync(rediscover: false).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolved.Address, Is.EqualTo(IPAddress.Parse("192.168.0.111")));
            Assert.That(resolved.Port, Is.EqualTo(21106));
            Assert.That(missing.Address, Is.Null);
            Assert.That(missing.Error, Is.EqualTo("No Z21 assigned to project 'Yard'"));
            Assert.That(blocked.Address, Is.Null);
            Assert.That(blocked.Error, Is.EqualTo("Z21 is used by 'Station'"));
        }
    }

    private static Project Project(string name, string address, int port = Z21Endpoint.DefaultPort) =>
        new() { Name = name, Z21 = { IpAddress = address, Port = port } };
}
