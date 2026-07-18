// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service.Validation;
using global::Moba.Common.Multiplex;
using global::Moba.Domain;

internal sealed class ProjectDiagnosticsServiceTests
{
    private readonly ProjectDiagnosticsService _service = new(
        new DigitalAddressConflictDetector(new DefaultMultiplexerProvider()));

    [Test]
    public void Analyze_AllowsSharedWagonFunctionDecoderAddresses()
    {
        var project = new Project
        {
            PassengerWagons =
            [
                new PassengerWagon { Name = "Coach one", DigitalAddress = 20 },
                new PassengerWagon { Name = "Coach two", DigitalAddress = 20 }
            ],
            GoodsWagons = [new GoodsWagon { Name = "Guard van", DigitalAddress = 20 }]
        };

        Assert.That(_service.Analyze(project), Is.Empty);
    }

    [Test]
    public void Analyze_TreatsDuplicateLocomotiveAddressAsWarning()
    {
        var project = new Project
        {
            Locomotives =
            [
                new Locomotive { Name = "Lead", DigitalAddress = 7 },
                new Locomotive { Name = "Helper", DigitalAddress = 7 }
            ]
        };

        var diagnostic = _service.Analyze(project).Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(ProjectDiagnosticSeverity.Warning));
            Assert.That(diagnostic.Source, Is.EqualTo("Locomotives"));
            Assert.That(diagnostic.TargetIds, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Analyze_RequiresSignalBoxFeedbackPointsToHaveUniqueInPorts()
    {
        var project = new Project
        {
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements =
                [
                    new SbDetector { Name = "Missing", FeedbackAddress = 0 },
                    new SbDetector { Name = "Block one", FeedbackAddress = 5 },
                    new SbDetector { Name = "Block two", FeedbackAddress = 5 }
                ]
            }
        };

        var diagnostics = _service.Analyze(project);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Count.EqualTo(2));
            Assert.That(diagnostics.All(
                diagnostic => diagnostic.Severity == ProjectDiagnosticSeverity.Error), Is.True);
            Assert.That(diagnostics.Any(diagnostic => diagnostic.Message.Contains("no InPort", StringComparison.Ordinal)), Is.True);
            Assert.That(diagnostics.Any(diagnostic => diagnostic.TargetIds.Count == 2), Is.True);
        });
    }

    [Test]
    public void Analyze_RequiresPositiveUniqueTrackPlanFeedbackInPorts()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var invalid = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var project = new Project
        {
            TrackPlan = new TrackPlanDocument
            {
                Segments =
                [
                    new TrackPlanSegment { Id = first, Code = "G239", InPort = 12 },
                    new TrackPlanSegment { Id = second, Code = "G231", InPort = 12 },
                    new TrackPlanSegment { Id = invalid, Code = "R9", InPort = 0 },
                    new TrackPlanSegment { Id = Guid.NewGuid(), Code = "WR", InPort = null }
                ]
            }
        };

        var diagnostics = _service.Analyze(project);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Count.EqualTo(2));
            Assert.That(diagnostics.All(
                diagnostic => diagnostic.Severity == ProjectDiagnosticSeverity.Error), Is.True);
            Assert.That(diagnostics.Any(diagnostic => diagnostic.TargetIds.SequenceEqual(new[] { first, second })), Is.True);
            Assert.That(diagnostics.Any(diagnostic => diagnostic.TargetIds.SequenceEqual(new[] { invalid })), Is.True);
        });
    }

    [Test]
    public void Analyze_ReturnsErrorsBeforeWarningsWithStableIds()
    {
        var locomotiveId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondLocomotiveId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var detectorId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var project = new Project
        {
            Locomotives =
            [
                new Locomotive { Id = locomotiveId, Name = "One", DigitalAddress = 8 },
                new Locomotive { Id = secondLocomotiveId, Name = "Two", DigitalAddress = 8 }
            ],
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements = [new SbDetector { Id = detectorId, Name = "Missing", FeedbackAddress = 0 }]
            }
        };

        var first = _service.Analyze(project);
        var second = _service.Analyze(project);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(item => item.Severity), Is.EqualTo(new[]
            {
                ProjectDiagnosticSeverity.Error,
                ProjectDiagnosticSeverity.Warning
            }));
            Assert.That(second.Select(item => item.Id), Is.EqualTo(first.Select(item => item.Id)));
        });
    }
}
