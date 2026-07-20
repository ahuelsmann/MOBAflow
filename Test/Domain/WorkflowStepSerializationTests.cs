// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;

using System.Text.Json;

[TestFixture]
internal sealed class WorkflowStepSerializationTests
{
    [Test]
    public void SerializeDeserialize_AllStepKinds_PreserveExplicitDiscriminatorsAndPayloads()
    {
        // Arrange
        var terminalId = Guid.NewGuid();
        WorkflowStep[] steps =
        [
            new WorkflowActionStep
            {
                Name = "Command",
                NextStepId = terminalId,
                Action = new WorkflowAction
                {
                    Type = ActionType.Command,
                    Command = new CommandActionPayload { Address = 3 }
                }
            },
            new WorkflowDelayStep { Name = "Wait", DelayMs = 250, NextStepId = terminalId },
            new WorkflowConditionStep
            {
                Name = "At station",
                Condition = new CurrentStationWorkflowCondition { StationId = Guid.NewGuid() },
                TrueStepId = terminalId,
                FalseStepId = terminalId
            },
            new WorkflowParallelStep
            {
                Name = "Parallel",
                JoinStepId = terminalId,
                Branches = [new WorkflowParallelBranch { Name = "Primary", EntryStepId = terminalId }]
            },
            new WorkflowNestedStep { Name = "Child", WorkflowId = Guid.NewGuid(), NextStepId = terminalId },
            new WorkflowTerminateStep { Id = terminalId, Name = "Done", Result = WorkflowTerminationResult.Succeeded }
        ];

        // Act
        var json = JsonSerializer.Serialize(steps, JsonOptions.Default);
        var roundTripped = JsonSerializer.Deserialize<WorkflowStep[]>(json, JsonOptions.Default);

        // Assert
        Assert.That(roundTripped, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(roundTripped, Has.Length.EqualTo(6));
            Assert.That(roundTripped![0], Is.TypeOf<WorkflowActionStep>());
            Assert.That(roundTripped[1], Is.TypeOf<WorkflowDelayStep>());
            Assert.That(roundTripped[2], Is.TypeOf<WorkflowConditionStep>());
            Assert.That(((WorkflowConditionStep)roundTripped[2]).Condition, Is.TypeOf<CurrentStationWorkflowCondition>());
            Assert.That(roundTripped[3], Is.TypeOf<WorkflowParallelStep>());
            Assert.That(roundTripped[4], Is.TypeOf<WorkflowNestedStep>());
            Assert.That(roundTripped[5], Is.TypeOf<WorkflowTerminateStep>());
            Assert.That(json, Does.Contain("\"kind\": \"action\""));
            Assert.That(json, Does.Contain("\"kind\": \"currentStation\""));
        });
    }

    [Test]
    public void Deserialize_UnknownStepKind_ThrowsJsonException()
    {
        const string json = """
            {
              "kind": "unsupported",
              "id": "11111111-1111-1111-1111-111111111111",
              "name": "Unknown"
            }
            """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkflowStep>(json, JsonOptions.Default));
    }
}
