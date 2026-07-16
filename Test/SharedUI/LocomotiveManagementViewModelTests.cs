// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Service;
using global::Moba.Backend.Service.Validation;
using global::Moba.Common.Multiplex;
using global::Moba.Domain;
using global::Moba.SharedUI.ViewModel;
using global::Moba.SharedUI.Interface;
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

    [Test]
    public async Task ExportCommands_WriteEscapedPassportAndDeterministicCvBackup()
    {
        var detector = new Mock<IDigitalAddressConflictDetector>();
        detector.Setup(candidate => candidate.Detect(It.IsAny<Project>()))
            .Returns(new DigitalAddressConflictReport([], []));
        var passportPath = Path.Combine(Path.GetTempPath(), $"passport-{Guid.NewGuid():N}.html");
        var cvPath = Path.Combine(Path.GetTempPath(), $"cv-{Guid.NewGuid():N}.json");
        var picker = new Mock<IFilePickerService>();
        picker.Setup(candidate => candidate.SaveHtmlFileAsync(It.IsAny<string>())).ReturnsAsync(passportPath);
        picker.Setup(candidate => candidate.SaveJsonFileAsync(It.IsAny<string>())).ReturnsAsync(cvPath);
        var locomotive = new Locomotive
        {
            Name = "<Depot>",
            Decoder = new LocomotiveDecoderProfile
            {
                Protocol = DecoderProtocol.Dcc,
                CvSnapshots = [new DecoderCvSnapshot { Name = "Current", Values = [new DecoderCvValue { Number = 29, Value = 6 }] }]
            }
        };
        var project = new Project { Locomotives = [locomotive] };
        var viewModel = new LocomotiveManagementViewModel(
            detector.Object,
            new LocomotiveMaintenanceService(),
            new LocomotiveLibraryService(),
            new LocomotivePassportHtmlRenderer(),
            new DecoderCvService(),
            picker.Object);
        viewModel.SetContext(project, locomotive);

        try
        {
            await viewModel.ExportPassportCommand.ExecuteAsync(null);
            await viewModel.ExportLatestCvSnapshotCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(passportPath), Does.Contain("&lt;Depot&gt;"));
                Assert.That(File.ReadAllText(cvPath), Does.Contain("\"number\": 29"));
                Assert.That(viewModel.OperationStatus, Is.EqualTo("CV backup exported."));
            });
        }
        finally
        {
            File.Delete(passportPath);
            File.Delete(cvPath);
        }
    }

    [Test]
    public async Task ImportCvSnapshot_ValidatesBeforeAddingToProfile()
    {
        var detector = new Mock<IDigitalAddressConflictDetector>();
        detector.Setup(candidate => candidate.Detect(It.IsAny<Project>()))
            .Returns(new DigitalAddressConflictReport([], []));
        var path = Path.Combine(Path.GetTempPath(), $"cv-import-{Guid.NewGuid():N}.json");
        var cvService = new DecoderCvService();
        await File.WriteAllTextAsync(path, cvService.Export(new DecoderCvSnapshot
        {
            Name = "Imported",
            Values = [new DecoderCvValue { Number = 1, Value = 7 }]
        }));
        var picker = new Mock<IFilePickerService>();
        picker.Setup(candidate => candidate.BrowseForJsonFileAsync()).ReturnsAsync(path);
        var locomotive = new Locomotive();
        var project = new Project { Locomotives = [locomotive] };
        var viewModel = new LocomotiveManagementViewModel(
            detector.Object,
            new LocomotiveMaintenanceService(),
            new LocomotiveLibraryService(),
            decoderCvService: cvService,
            filePicker: picker.Object);
        viewModel.SetContext(project, locomotive);

        try
        {
            await viewModel.ImportCvSnapshotCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(locomotive.Decoder!.CvSnapshots.Single().Name, Is.EqualTo("Imported"));
                Assert.That(viewModel.Passport!.DecoderSnapshotCount, Is.EqualTo(1));
                Assert.That(viewModel.OperationStatus, Is.EqualTo("CV backup imported."));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }
}
