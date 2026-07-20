// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Domain;
using Moba.Domain.Enum;

[TestFixture]
internal sealed class WorkflowValidatorTests
{
    private readonly WorkflowValidator _validator = new();

    [Test]
    public void Validate_ValidLinearWorkflow_ReturnsValidResult()
    {
        // Arrange
        var workflow = CreateValidWorkflow();

        // Act
        var result = _validator.Validate(new Project { Workflows = [workflow] });

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void Validate_MissingSuccessorAndUnreachableStep_ReturnsNavigationReadyIssues()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        var actionStep = (WorkflowActionStep)workflow.Steps![0];
        actionStep.NextStepId = Guid.NewGuid();

        // Act
        var result = _validator.Validate(new Project { Workflows = [workflow] });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.Code == WorkflowValidationCodes.MissingStepReference &&
                issue.WorkflowId == workflow.Id &&
                issue.StepId == actionStep.Id &&
                issue.FieldPath == "nextStepId"), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == WorkflowValidationCodes.UnreachableStep), Is.True);
        });
    }

    [Test]
    public void Validate_GraphCycle_ReturnsGraphCycleIssue()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        var actionStep = (WorkflowActionStep)workflow.Steps![0];
        var terminalStep = (WorkflowTerminateStep)workflow.Steps[1];
        workflow.Steps[1] = new WorkflowDelayStep
        {
            Id = terminalStep.Id,
            Name = "Loop",
            NextStepId = actionStep.Id
        };

        // Act
        var result = _validator.Validate(new Project { Workflows = [workflow] });

        // Assert
        Assert.That(result.Issues.Any(issue => issue.Code == WorkflowValidationCodes.GraphCycle), Is.True);
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Validate_RetryAttemptsOutsideBounds_ReturnsRetryIssue(int additionalAttempts)
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        workflow.DefaultErrorPolicy = new WorkflowErrorPolicy
        {
            Retry = new WorkflowRetryPolicy { AdditionalAttempts = additionalAttempts }
        };

        // Act
        var result = _validator.Validate(new Project { Workflows = [workflow] });

        // Assert
        Assert.That(result.Issues.Any(issue => issue.Code == WorkflowValidationCodes.InvalidRetryPolicy), Is.True);
    }

    [Test]
    public void Validate_IndirectNestedWorkflowCycle_ReturnsNestedCycleIssue()
    {
        // Arrange
        var first = CreateNestedWorkflow();
        var second = CreateNestedWorkflow();
        ((WorkflowNestedStep)first.Steps![0]).WorkflowId = second.Id;
        ((WorkflowNestedStep)second.Steps![0]).WorkflowId = first.Id;

        // Act
        var result = _validator.Validate(new Project { Workflows = [first, second] });

        // Assert
        Assert.That(result.Issues.Any(issue => issue.Code == WorkflowValidationCodes.NestedWorkflowCycle), Is.True);
    }

    [Test]
    public void Validate_ParallelBranchesOverlapBeforeJoin_ReturnsOverlapIssue()
    {
        // Arrange
        var parallelId = Guid.NewGuid();
        var firstBranchId = Guid.NewGuid();
        var secondBranchId = Guid.NewGuid();
        var sharedId = Guid.NewGuid();
        var joinId = Guid.NewGuid();
        var workflow = new Workflow
        {
            EntryStepId = parallelId,
            Steps =
            [
                new WorkflowParallelStep
                {
                    Id = parallelId,
                    JoinStepId = joinId,
                    Branches =
                    [
                        new WorkflowParallelBranch { Name = "First", EntryStepId = firstBranchId },
                        new WorkflowParallelBranch { Name = "Second", EntryStepId = secondBranchId }
                    ]
                },
                new WorkflowDelayStep { Id = firstBranchId, DelayMs = 1, NextStepId = sharedId },
                new WorkflowDelayStep { Id = secondBranchId, DelayMs = 1, NextStepId = sharedId },
                new WorkflowDelayStep { Id = sharedId, DelayMs = 1, NextStepId = joinId },
                new WorkflowTerminateStep { Id = joinId }
            ]
        };

        // Act
        var result = _validator.Validate(new Project { Workflows = [workflow] });

        // Assert
        Assert.That(result.Issues.Any(issue => issue.Code == WorkflowValidationCodes.OverlappingParallelBranches), Is.True);
    }

    [Test]
    public void Validate_UnsupportedActionPayload_ReturnsPayloadIssue()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        ((WorkflowActionStep)workflow.Steps![0]).Action = new WorkflowAction { Type = ActionType.Matrix };

        // Act
        var result = _validator.Validate(new Project { Workflows = [workflow] });

        // Assert
        Assert.That(result.Issues.Any(issue => issue.Code == WorkflowValidationCodes.InvalidStepPayload), Is.True);
    }

    [Test]
    public void Validate_ParallelBranchesWriteSameExclusiveResource_ReturnsConflictIssue()
    {
        // Arrange
        var parallelId = Guid.NewGuid();
        var firstBranchId = Guid.NewGuid();
        var secondBranchId = Guid.NewGuid();
        var joinId = Guid.NewGuid();
        var workflow = new Workflow
        {
            EntryStepId = parallelId,
            Steps =
            [
                new WorkflowParallelStep
                {
                    Id = parallelId,
                    JoinStepId = joinId,
                    Branches =
                    [
                        new WorkflowParallelBranch { EntryStepId = firstBranchId },
                        new WorkflowParallelBranch { EntryStepId = secondBranchId }
                    ]
                },
                CreateAudioStep(firstBranchId, joinId, "first.wav"),
                CreateAnnouncementStep(secondBranchId, joinId),
                new WorkflowTerminateStep { Id = joinId }
            ]
        };

        // Act
        var result = _validator.Validate(new Project { Workflows = [workflow] });

        // Assert
        Assert.That(result.Issues.Any(issue =>
            issue.Code == WorkflowValidationCodes.ConflictingParallelResource &&
            issue.StepId == parallelId), Is.True);
    }

    private static Workflow CreateValidWorkflow()
    {
        var actionId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        return new Workflow
        {
            EntryStepId = actionId,
            Steps =
            [
                new WorkflowActionStep
                {
                    Id = actionId,
                    Name = "Set speed",
                    NextStepId = terminalId,
                    Action = new WorkflowAction
                    {
                        Type = ActionType.Command,
                        Command = new CommandActionPayload { BytesBase64 = "AQID" }
                    }
                },
                new WorkflowTerminateStep
                {
                    Id = terminalId,
                    Name = "Done",
                    Result = WorkflowTerminationResult.Succeeded
                }
            ]
        };
    }

    private static WorkflowActionStep CreateAudioStep(Guid id, Guid nextStepId, string filePath) =>
        new()
        {
            Id = id,
            NextStepId = nextStepId,
            Action = new WorkflowAction
            {
                Type = ActionType.Audio,
                Audio = new AudioActionPayload { FilePath = filePath }
            }
        };

    private static WorkflowActionStep CreateAnnouncementStep(Guid id, Guid nextStepId) =>
        new()
        {
            Id = id,
            NextStepId = nextStepId,
            Action = new WorkflowAction
            {
                Type = ActionType.Announcement,
                Announcement = new AnnouncementActionPayload { Message = "Next stop" }
            }
        };

    private static Workflow CreateNestedWorkflow()
    {
        var nestedId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        return new Workflow
        {
            EntryStepId = nestedId,
            Steps =
            [
                new WorkflowNestedStep { Id = nestedId, NextStepId = terminalId },
                new WorkflowTerminateStep { Id = terminalId }
            ]
        };
    }
}
