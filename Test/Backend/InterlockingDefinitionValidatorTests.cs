// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Service.Validation;
using global::Moba.Domain;

internal sealed class InterlockingDefinitionValidatorTests
{
    private readonly InterlockingDefinitionValidator _validator = new();

    [Test]
    public void Validate_CompleteDefinition_ReturnsValidReport()
    {
        var project = CreateValidProject();

        var report = _validator.Validate(project);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Findings, Is.Empty);
        });
    }

    [Test]
    public void Validate_BindingToUnknownPresentation_ReturnsBothMissingReferences()
    {
        var project = CreateValidProject();
        var binding = project.Interlocking.Bindings.Single();
        binding.TrackSegmentIds = [Guid.Parse("00000000-0000-0000-0000-000000000091")];
        binding.SignalBoxElementIds = [Guid.Parse("00000000-0000-0000-0000-000000000092")];

        var report = _validator.Validate(project);

        Assert.That(
            report.Findings.Select(finding => finding.Code),
            Is.SupersetOf(new[] { "binding.track.missing", "binding.signalbox.missing" }));
    }

    [Test]
    public void Validate_MissingClearObservation_ReturnsActionableFinding()
    {
        var project = CreateValidProject();
        project.Interlocking.Blocks.Single().FeedbackInputs.RemoveAll(input => input.Role == BlockFeedbackRole.Clear);

        var report = _validator.Validate(project);

        Assert.That(
            report.Findings.Select(finding => finding.Code),
            Does.Contain("block.feedback.clear.missing"));
    }

    private static Project CreateValidProject()
    {
        var turnoutId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var signalId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var blockId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var trackSegmentId = Guid.Parse("00000000-0000-0000-0000-000000000005");
        var signalBoxElementId = Guid.Parse("00000000-0000-0000-0000-000000000006");

        return new Project
        {
            TrackPlan = new TrackPlanDocument
            {
                Segments = [new TrackPlanSegment { Id = trackSegmentId, Code = "WR" }]
            },
            SignalBoxPlan = new SignalBoxPlan
            {
                Elements = [new SbSwitch { Id = signalBoxElementId, Name = "W1" }]
            },
            Interlocking = new InterlockingDefinition
            {
                Turnouts =
                [
                    new TurnoutDefinition
                    {
                        Id = turnoutId,
                        Name = "W1",
                        DecoderAddress = 10,
                        Commands =
                        [
                            new TurnoutCommandMapping
                            {
                                Position = TurnoutPosition.Straight,
                                Commands = [new TurnoutAccessoryCommand { Output = 0 }]
                            },
                            new TurnoutCommandMapping
                            {
                                Position = TurnoutPosition.DivergingLeft,
                                Commands = [new TurnoutAccessoryCommand { Output = 1 }]
                            }
                        ]
                    }
                ],
                Signals =
                [
                    new SignalDefinition { Id = signalId, Name = "N1", BaseAddress = 20 }
                ],
                Blocks =
                [
                    new BlockDefinition
                    {
                        Id = blockId,
                        Name = "Platform",
                        BoundaryElementIds = [turnoutId, signalId],
                        FeedbackInputs =
                        [
                            new BlockFeedbackInput
                            {
                                InPort = 1,
                                Role = BlockFeedbackRole.Occupied,
                                ActiveState = true
                            },
                            new BlockFeedbackInput
                            {
                                InPort = 1,
                                Role = BlockFeedbackRole.Clear,
                                ActiveState = false
                            }
                        ]
                    }
                ],
                Connections =
                [
                    new OperationalConnection { FromOperationalId = signalId, ToOperationalId = turnoutId },
                    new OperationalConnection { FromOperationalId = turnoutId, ToOperationalId = blockId }
                ],
                Bindings =
                [
                    new OperationalBinding
                    {
                        OperationalId = turnoutId,
                        TrackSegmentIds = [trackSegmentId],
                        SignalBoxElementIds = [signalBoxElementId]
                    }
                ]
            }
        };
    }
}
