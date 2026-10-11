// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Common.Discovery;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

using Moq;

/// <summary>
/// Covers the Z21 finder: every found Z21 is listed until a project uses it, and each Z21 belongs to one project.
/// </summary>
[TestFixture]
internal sealed class Z21AssignmentViewModelTests
{
    private static readonly string[] BothAddresses = ["192.168.0.111", "192.168.0.112"];
    private static readonly string[] SecondAddress = ["192.168.0.112"];

    [Test]
    public async Task Search_ListsOnlyZ21sThatNoProjectUses()
    {
        var solution = TwoProjects(firstProjectZ21: "192.168.0.111");
        var viewModel = Create(solution, out _);

        await viewModel.SearchCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.UnassignedZ21s.Select(z21 => z21.IpAddress), Is.EqualTo(SecondAddress));
            Assert.That(viewModel.UnassignedZ21s[0].Detail, Is.EqualTo("Serial number 2002, port 21105"));
            Assert.That(viewModel.StatusText, Is.EqualTo("2 Z21 found; 1 not assigned yet."));
        }
    }

    [Test]
    public async Task AssignToProject_SelectsTheProjectStoresTheZ21AndReportsTheChange()
    {
        var solution = TwoProjects(firstProjectZ21: string.Empty);
        var viewModel = Create(solution, out var session);
        await viewModel.SearchCommand.ExecuteAsync(null).ConfigureAwait(false);
        var target = session.SolutionViewModel!.Projects[1];
        var changes = new List<string?>();
        session.ModelChanged += (_, e) => changes.Add(e.PropertyName);

        var assigned = viewModel.AssignToProject(viewModel.UnassignedZ21s[0], target);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(assigned, Is.True);
            Assert.That(session.SelectedProject, Is.SameAs(target));
            Assert.That(target.Model.Z21.IpAddress, Is.EqualTo("192.168.0.111"));
            Assert.That(target.Model.Z21.Port, Is.EqualTo(21105));
            Assert.That(target.Model.Z21.SerialNumber, Is.EqualTo(1001u));
            Assert.That(changes, Does.Contain(nameof(ProjectViewModel.Z21IpAddress)), "The session saves the assignment.");
            Assert.That(viewModel.UnassignedZ21s.Select(z21 => z21.IpAddress), Is.EqualTo(SecondAddress));
        }
    }

    [Test]
    public async Task AssignToProject_RefusesAZ21ThatAnotherProjectUses()
    {
        var solution = TwoProjects(firstProjectZ21: string.Empty);
        var viewModel = Create(solution, out var session);
        await viewModel.SearchCommand.ExecuteAsync(null).ConfigureAwait(false);
        var candidate = viewModel.UnassignedZ21s[0];
        var first = session.SolutionViewModel!.Projects[0];
        var second = session.SolutionViewModel.Projects[1];
        viewModel.AssignToProject(candidate, first);

        var assigned = viewModel.AssignToProject(candidate, second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(assigned, Is.False);
            Assert.That(second.Z21IpAddress, Is.Empty);
            Assert.That(first.Z21IpAddress, Is.EqualTo("192.168.0.111"));
        }
    }

    [Test]
    public async Task RemoveZ21_MakesTheZ21AvailableAgain()
    {
        var solution = TwoProjects(firstProjectZ21: "192.168.0.111");
        var viewModel = Create(solution, out var session);
        await viewModel.SearchCommand.ExecuteAsync(null).ConfigureAwait(false);
        var first = session.SolutionViewModel!.Projects[0];
        session.SelectedProject = first;

        first.RemoveZ21Command.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Model.Z21.IpAddress, Is.Empty);
            Assert.That(first.RemoveZ21Command.CanExecute(null), Is.False);
            Assert.That(viewModel.UnassignedZ21s.Select(z21 => z21.IpAddress), Is.EqualTo(BothAddresses));
        }
    }

    [Test]
    public async Task Search_ReportsAFailureWithoutListingStaleResults()
    {
        var discovery = new Mock<IZ21DiscoveryService>();
        discovery.Setup(value => value.DiscoverAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        var session = CreateSession(TwoProjects(firstProjectZ21: string.Empty));
        var viewModel = new Z21AssignmentViewModel(session, discovery.Object, NullLogger<Z21AssignmentViewModel>.Instance);

        await viewModel.SearchCommand.ExecuteAsync(null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(viewModel.UnassignedZ21s, Is.Empty);
            Assert.That(viewModel.StatusText, Is.EqualTo("Z21 search failed: network down"));
            Assert.That(viewModel.IsSearching, Is.False);
        }
    }

    [Test]
    public void ChangingTheIpAddress_ForgetsTheSerialNumber()
    {
        var project = new ProjectViewModel(new Project { Z21 = { IpAddress = "192.168.0.111", SerialNumber = 1001 } });

        project.Z21IpAddress = " 192.168.0.120 ";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(project.Model.Z21.IpAddress, Is.EqualTo("192.168.0.120"));
            Assert.That(project.Model.Z21.SerialNumber, Is.Null);
            Assert.That(project.Z21Summary, Is.EqualTo("Z21 192.168.0.120:21105"));
        }
    }

    private static Solution TwoProjects(string firstProjectZ21) => new()
    {
        Projects =
        [
            new Project { Name = "Station", Z21 = { IpAddress = firstProjectZ21 } },
            new Project { Name = "Yard" }
        ]
    };

    private static Z21AssignmentViewModel Create(Solution solution, out SolutionSession session)
    {
        var discovery = new Mock<IZ21DiscoveryService>();
        discovery.Setup(value => value.DiscoverAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new DiscoveredZ21("192.168.0.111", 21105, 1001),
                new DiscoveredZ21("192.168.0.112", 21105, 2002)
            ]);
        session = CreateSession(solution);
        return new Z21AssignmentViewModel(session, discovery.Object, NullLogger<Z21AssignmentViewModel>.Instance);
    }

    private static SolutionSession CreateSession(Solution solution)
    {
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        return new SolutionSession(
            solution,
            new Mock<IIoService>().Object,
            dispatcher.Object,
            new Mock<IProjectRuntimeHost>().Object,
            NullLogger<SolutionSession>.Instance);
    }
}
