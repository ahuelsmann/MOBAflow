// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Service;
using global::Moba.Backend.Service.Validation;
using global::Moba.Common.Multiplex;
using global::Moba.Domain;
using global::Moba.SharedUI.ViewModel;
using Moq;

internal sealed class LocomotiveManagementViewModelTests
{
    [Test]
    public void SetContext_ProjectsConflictTargetsAndSelectedLocomotiveDetails()
    {
        var multiplexer = new Mock<IMultiplexerProvider>();
        var detector = new DigitalAddressConflictDetector(multiplexer.Object);
        var maintenanceService = new LocomotiveMaintenanceService();
        var locomotive = new Locomotive
        {
            Name = "First",
            DigitalAddress = 5,
            Maintenance = new LocomotiveMaintenanceData
            {
                Plans = [new LocomotiveMaintenancePlan { Name = "Annual", LastCompletedAt = new DateTimeOffset(2025, 7, 16, 0, 0, 0, TimeSpan.Zero), IntervalDays = 365 }]
            },
            Decoder = new LocomotiveDecoderProfile
            {
                CvSnapshots = [new DecoderCvSnapshot { Name = "Current" }]
            }
        };
        var conflicting = new Locomotive { Name = "Second", DigitalAddress = 5 };
        var project = new Project { Locomotives = [locomotive, conflicting] };
        var viewModel = new LocomotiveManagementViewModel(detector, maintenanceService, new LocomotiveLibraryService(maintenanceService));

        viewModel.SetContext(project, locomotive, new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.AddressFindings, Has.Count.EqualTo(1));
            Assert.That(viewModel.AddressFindings[0].TargetIds, Is.EquivalentTo(new[] { locomotive.Id, conflicting.Id }));
            Assert.That(viewModel.MaintenancePlans.Single().State, Is.EqualTo(MaintenanceDueState.Due));
            Assert.That(viewModel.DecoderSnapshots.Single().Name, Is.EqualTo("Current"));
            Assert.That(viewModel.Passport!.LocomotiveId, Is.EqualTo(locomotive.Id));
        });
    }

    [Test]
    public void SetContext_ClearsPreviousState()
    {
        var detector = new Mock<IDigitalAddressConflictDetector>();
        detector.Setup(candidate => candidate.Detect(It.IsAny<Project>()))
            .Returns(new DigitalAddressConflictReport([], []));
        var viewModel = new LocomotiveManagementViewModel(
            detector.Object,
            new LocomotiveMaintenanceService(),
            new LocomotiveLibraryService());

        viewModel.SetContext(new Project(), new Locomotive());
        viewModel.SetContext(null, null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Passport, Is.Null);
            Assert.That(viewModel.AddressFindings, Is.Empty);
            Assert.That(viewModel.MaintenancePlans, Is.Empty);
            Assert.That(viewModel.DecoderSnapshots, Is.Empty);
        });
    }

    [Test]
    public void ManagementCommands_AddPersistedMaintenanceAndWhistleConfiguration()
    {
        var detector = new Mock<IDigitalAddressConflictDetector>();
        detector.Setup(candidate => candidate.Detect(It.IsAny<Project>()))
            .Returns(new DigitalAddressConflictReport([], []));
        var project = new Project();
        var locomotive = new Locomotive();
        project.Locomotives.Add(locomotive);
        var viewModel = new LocomotiveManagementViewModel(
            detector.Object,
            new LocomotiveMaintenanceService(),
            new LocomotiveLibraryService());
        viewModel.SetContext(project, locomotive);

        viewModel.AddMaintenancePlanCommand.Execute(null);
        viewModel.AddMaintenanceEntryCommand.Execute(null);
        viewModel.AddWhistleRuleCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(locomotive.Maintenance!.Plans, Has.Count.EqualTo(1));
            Assert.That(locomotive.Maintenance.Entries, Has.Count.EqualTo(1));
            Assert.That(project.LocomotiveWhistleRules.Single().LocomotiveId, Is.EqualTo(locomotive.Id));
            Assert.That(viewModel.WhistleRules, Has.Count.EqualTo(1));
        });
    }
}
