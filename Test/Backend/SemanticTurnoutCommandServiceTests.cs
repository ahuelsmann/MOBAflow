// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service.Interlocking;
using global::Moba.Domain;

using Moq;

internal sealed class SemanticTurnoutCommandServiceTests
{
    [Test]
    public async Task ExecuteAsync_ThreeWayPosition_DispatchesConfiguredSequenceInOrder()
    {
        var turnoutId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var gateway = new RecordingTurnoutEffectGateway();
        var service = new SemanticTurnoutCommandService(CreateDefinition(turnoutId), gateway);
        var correlationId = Guid.NewGuid();

        var result = await service.ExecuteAsync(
            turnoutId,
            TurnoutPosition.DivergingRight,
            correlationId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TurnoutCommandExecutionStatus.Succeeded));
            Assert.That(result.RequiresReconciliation, Is.False);
            Assert.That(gateway.Commands.Select(command => command.DecoderAddress), Is.EqualTo(new[] { 100, 101 }));
            Assert.That(gateway.Commands.Select(command => command.Output), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(gateway.Commands.Select(command => command.SequenceIndex), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(gateway.Commands.Select(command => command.Queue), Is.EqualTo(new[] { true, false }));
            Assert.That(gateway.Commands.All(command => command.CorrelationId == correlationId), Is.True);
        });
    }

    [Test]
    public async Task ExecuteAsync_MissingOrInvalidMapping_RejectsBeforeGatewayCall()
    {
        var turnoutId = Guid.NewGuid();
        var definition = CreateDefinition(turnoutId);
        definition.Turnouts.Single().Commands.RemoveAll(mapping => mapping.Position == TurnoutPosition.DivergingLeft);
        definition.Turnouts.Single().Commands.Single(mapping => mapping.Position == TurnoutPosition.Straight)
            .Commands.Single().AddressOffset = -1;
        var gateway = new RecordingTurnoutEffectGateway();
        var service = new SemanticTurnoutCommandService(definition, gateway);

        var missing = await service.ExecuteAsync(turnoutId, TurnoutPosition.DivergingLeft, Guid.NewGuid());
        var invalid = await service.ExecuteAsync(turnoutId, TurnoutPosition.Straight, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code, Is.EqualTo("turnout.mapping.missing"));
            Assert.That(invalid.Code, Is.EqualTo("turnout.mapping.invalid"));
            Assert.That(gateway.Commands, Is.Empty);
        });
    }

    [Test]
    public async Task ExecuteAsync_OfflineGateway_DoesNotRequireReconciliation()
    {
        var turnoutId = Guid.NewGuid();
        var gateway = new RecordingTurnoutEffectGateway(
            _ => new TurnoutEffectResult(TurnoutEffectStatus.Offline, "Offline"));
        var service = new SemanticTurnoutCommandService(CreateDefinition(turnoutId), gateway);

        var result = await service.ExecuteAsync(turnoutId, TurnoutPosition.Straight, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TurnoutCommandExecutionStatus.Offline));
            Assert.That(result.DispatchedCommands, Is.Empty);
            Assert.That(result.RequiresReconciliation, Is.False);
        });
    }

    [Test]
    public async Task ExecuteAsync_PartialFailure_StopsSequenceAndRequiresReconciliation()
    {
        var turnoutId = Guid.NewGuid();
        var gateway = new RecordingTurnoutEffectGateway(command =>
            command.SequenceIndex == 1
                ? new TurnoutEffectResult(TurnoutEffectStatus.Failed, "Injected failure")
                : new TurnoutEffectResult(TurnoutEffectStatus.Succeeded));
        var service = new SemanticTurnoutCommandService(CreateDefinition(turnoutId), gateway);

        var result = await service.ExecuteAsync(
            turnoutId,
            TurnoutPosition.DivergingRight,
            Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TurnoutCommandExecutionStatus.Failed));
            Assert.That(result.DispatchedCommands, Has.Count.EqualTo(2));
            Assert.That(result.RequiresReconciliation, Is.True);
            Assert.That(gateway.Commands, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task ExecuteAsync_PreCancelledToken_DoesNotInvokeGateway()
    {
        var turnoutId = Guid.NewGuid();
        var gateway = new RecordingTurnoutEffectGateway();
        var service = new SemanticTurnoutCommandService(CreateDefinition(turnoutId), gateway);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await service.ExecuteAsync(
            turnoutId,
            TurnoutPosition.Straight,
            Guid.NewGuid(),
            cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TurnoutCommandExecutionStatus.Cancelled));
            Assert.That(gateway.Commands, Is.Empty);
        });
    }

    [Test]
    public async Task Z21Gateway_WhenDisconnected_ReturnsOfflineWithoutRawCommand()
    {
        var z21 = new Mock<IZ21>();
        z21.SetupGet(candidate => candidate.IsConnected).Returns(false);
        var gateway = new Z21TurnoutEffectGateway(z21.Object);
        var command = new TurnoutEffectCommand(
            Guid.NewGuid(),
            TurnoutPosition.Straight,
            100,
            0,
            true,
            false,
            0,
            Guid.NewGuid());

        var result = await gateway.ExecuteAsync(command);

        Assert.That(result.Status, Is.EqualTo(TurnoutEffectStatus.Offline));
        z21.Verify(
            candidate => candidate.SetTurnoutAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static InterlockingDefinition CreateDefinition(Guid turnoutId) =>
        new()
        {
            Turnouts =
            [
                new TurnoutDefinition
                {
                    Id = turnoutId,
                    Name = "W1",
                    DecoderAddress = 100,
                    Kind = TurnoutKind.ThreeWay,
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
                        },
                        new TurnoutCommandMapping
                        {
                            Position = TurnoutPosition.DivergingRight,
                            Commands =
                            [
                                new TurnoutAccessoryCommand { AddressOffset = 0, Output = 1, Queue = true },
                                new TurnoutAccessoryCommand { AddressOffset = 1, Output = 0 }
                            ]
                        }
                    ]
                }
            ]
        };
}
