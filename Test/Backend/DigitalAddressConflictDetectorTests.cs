// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service.Validation;
using global::Moba.Common.Multiplex;
using global::Moba.Domain;

internal sealed class DigitalAddressConflictDetectorTests
{
    private readonly DigitalAddressConflictDetector _detector =
        new(new DefaultMultiplexerProvider());

    [Test]
    public void Detect_ReportsDuplicateLocomotiveAddresses()
    {
        var project = new Project
        {
            Locomotives =
            [
                new Locomotive { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "One", DigitalAddress = 7 },
                new Locomotive { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Two", DigitalAddress = 7 }
            ]
        };

        var report = _detector.Detect(project);

        var conflict = report.Findings.Single(finding => finding.Kind == DigitalAddressFindingKind.Conflict);
        Assert.Multiple(() =>
        {
            Assert.That(conflict.Domain, Is.EqualTo(DigitalAddressDomain.Locomotive));
            Assert.That(conflict.Start, Is.EqualTo(7));
            Assert.That(conflict.End, Is.EqualTo(7));
            Assert.That(conflict.Owners.Select(owner => owner.Id), Is.EquivalentTo(project.Locomotives.Select(locomotive => locomotive.Id)));
            Assert.That(conflict.Message, Does.Contain("traction group"));
        });
    }

    [Test]
    public void Detect_AllowsWagonsToShareAFunctionDecoderAddress()
    {
        var project = new Project
        {
            PassengerWagons =
            [
                new PassengerWagon { Name = "Coach one", DigitalAddress = 24 },
                new PassengerWagon { Name = "Coach two", DigitalAddress = 24 }
            ],
            GoodsWagons =
            [
                new GoodsWagon { Name = "Guard van", DigitalAddress = 24 }
            ]
        };

        var report = _detector.Detect(project);

        Assert.Multiple(() =>
        {
            Assert.That(report.Allocations, Is.Empty);
            Assert.That(report.Findings, Is.Empty);
            Assert.That(report.IsValid, Is.True);
        });
    }

    [Test]
    public void Detect_ReportsDuplicateLocomotivePrimaryAddressesInDoubleTraction()
    {
        var first = new Locomotive { Name = "Lead", DigitalAddress = 31 };
        var second = new Locomotive { Name = "Helper", DigitalAddress = 31 };
        var project = new Project
        {
            Locomotives = [first, second],
            Trains =
            [
                new Train
                {
                    Name = "Double traction",
                    IsDoubleTraction = true,
                    Vehicles =
                    [
                        new Vehicle
                        {
                            VehicleId = first.Id,
                            VehicleKind = global::Moba.Domain.Enum.TrainVehicleKind.Locomotive
                        },
                        new Vehicle
                        {
                            VehicleId = second.Id,
                            VehicleKind = global::Moba.Domain.Enum.TrainVehicleKind.Locomotive
                        }
                    ]
                }
            ]
        };

        var report = _detector.Detect(project);

        Assert.That(
            report.Findings.Single(finding => finding.Kind == DigitalAddressFindingKind.Conflict).Domain,
            Is.EqualTo(DigitalAddressDomain.Locomotive));
    }

    [Test]
    public void Detect_AllowsSameNumberInDifferentAddressDomains()
    {
        var project = new Project
        {
            Locomotives = [new Locomotive { Name = "Loco", DigitalAddress = 12 }],
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements =
                [
                    new SbSwitch { Name = "Switch", Address = 12 },
                    new SbDetector { Name = "Detector", FeedbackAddress = 12 }
                ]
            }
        };

        var report = _detector.Detect(project);

        Assert.Multiple(() =>
        {
            Assert.That(report.Allocations, Has.Count.EqualTo(3));
            Assert.That(report.HasConflicts, Is.False);
        });
    }

    [Test]
    public void Detect_IgnoresUnconfiguredAddresses()
    {
        var project = new Project
        {
            Locomotives = [new Locomotive { Name = "Unconfigured", DigitalAddress = null }],
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements =
                [
                    new SbSwitch { Name = "Switch", Address = 0 },
                    new SbSignal { Name = "Signal", BaseAddress = 0 },
                    new SbDetector { Name = "Detector", FeedbackAddress = 0 }
                ]
            }
        };

        var report = _detector.Detect(project);

        Assert.Multiple(() =>
        {
            Assert.That(report.Allocations, Is.Empty);
            Assert.That(report.Findings, Is.Empty);
        });
    }

    [Test]
    public void Detect_UsesEntireMultiplexerRange()
    {
        var signal = new SbSignal
        {
            Name = "N1",
            IsMultiplexed = true,
            MultiplexerArticleNumber = "5229",
            MainSignalArticleNumber = "4046",
            BaseAddress = 100
        };
        var sbSwitch = new SbSwitch { Name = "W1", Address = 103 };
        var project = new Project
        {
            SignalBoxPlan = new SignalBoxPlan { Elements = [signal, sbSwitch] }
        };

        var report = _detector.Detect(project);

        var conflict = report.Findings.Single(finding => finding.Kind == DigitalAddressFindingKind.Conflict);
        Assert.Multiple(() =>
        {
            Assert.That(conflict.Domain, Is.EqualTo(DigitalAddressDomain.Accessory));
            Assert.That(conflict.Start, Is.EqualTo(103));
            Assert.That(conflict.End, Is.EqualTo(103));
            Assert.That(report.Allocations.Single(allocation => allocation.Owner.Id == signal.Id).End, Is.EqualTo(103));
        });
    }

    [Test]
    public void Detect_ReportsProtocolBoundaryViolations()
    {
        var project = new Project
        {
            Locomotives = [new Locomotive { Name = "Too high", DigitalAddress = 10_000 }],
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements = [new SbSwitch { Name = "Too high", Address = 2045 }]
            }
        };

        var report = _detector.Detect(project);

        Assert.That(
            report.Findings.Where(finding => finding.Kind == DigitalAddressFindingKind.OutOfRange).ToArray(),
            Has.Length.EqualTo(2));
    }

    [Test]
    public void Detect_ReportsUnknownMultiplexerWithoutGuessingItsRange()
    {
        var signal = new SbSignal
        {
            Name = "Unknown decoder",
            IsMultiplexed = true,
            MultiplexerArticleNumber = "does-not-exist",
            MainSignalArticleNumber = "unknown",
            BaseAddress = 50
        };
        var project = new Project
        {
            SignalBoxPlan = new SignalBoxPlan { Elements = [signal] }
        };

        var report = _detector.Detect(project);

        Assert.Multiple(() =>
        {
            Assert.That(
                report.Findings.Single().Kind,
                Is.EqualTo(DigitalAddressFindingKind.UnknownMultiplexerMapping));
            Assert.That(report.Allocations.Single().Start, Is.EqualTo(report.Allocations.Single().End));
        });
    }

    [Test]
    public void Detect_ReturnsDeterministicFindingIdsAndOrder()
    {
        var first = new Locomotive
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "One",
            DigitalAddress = 8
        };
        var second = new Locomotive
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Name = "Two",
            DigitalAddress = 8
        };
        var project = new Project { Locomotives = [first, second] };

        var firstRun = _detector.Detect(project);
        project.Locomotives.Reverse();
        var secondRun = _detector.Detect(project);

        Assert.That(
            secondRun.Findings.Select(finding => finding.Id),
            Is.EqualTo(firstRun.Findings.Select(finding => finding.Id)));
    }
}
