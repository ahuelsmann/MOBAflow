// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Domain;
using Moba.Domain.Enum;

[TestFixture]
internal sealed class WorkflowValidatorTests
{
    private static WorkflowAction Command() => new() { Type = ActionType.Command, Command = new() { BytesBase64 = "AQID" } };
    private static WorkflowValidationResult Validate(Workflow workflow) => new WorkflowValidator().Validate(new Project { Workflows = [workflow] });

    [Test]
    public void ValidActions_NeedNoConnectionsOrTermination()
    {
        var result = Validate(new Workflow { Actions = [Command(), Command()] });
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void EmptyWorkflow_CannotExecute()
    {
        Assert.That(Validate(new Workflow()).Issues.Select(issue => issue.Code), Does.Contain(WorkflowValidationCodes.EmptyWorkflow));
    }

    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void NegativeDelay_IdentifiesAffectedAction(int delay)
    {
        var action = Command();
        action.DelayAfterMs = delay;
        var issue = Validate(new Workflow { Actions = [action] }).Issues.Single();
        Assert.Multiple(() =>
        {
            Assert.That(issue.Code, Is.EqualTo(WorkflowValidationCodes.InvalidActionPayload));
            Assert.That(issue.StepId, Is.EqualTo(action.Id));
            Assert.That(issue.FieldPath, Is.EqualTo("actions[0].delayAfterMs"));
        });
    }

    [Test]
    public void DuplicateActionIds_AreRejected()
    {
        var first = Command();
        var second = Command();
        second.Id = first.Id;
        Assert.That(Validate(new Workflow { Actions = [first, second] }).Issues.Select(issue => issue.Code), Does.Contain(WorkflowValidationCodes.DuplicateActionId));
    }

    [Test]
    public void EmptyActionId_IsRejected()
    {
        var action = Command();
        action.Id = Guid.Empty;
        Assert.That(Validate(new Workflow { Actions = [action] }).Issues.Select(issue => issue.Code), Does.Contain(WorkflowValidationCodes.EmptyActionId));
    }

    [Test]
    public void NullAction_IsRejectedWithoutThrowing()
    {
        Assert.That(Validate(new Workflow { Actions = [null!] }).IsValid, Is.False);
    }

    [TestCase(ActionType.Command)]
    [TestCase(ActionType.Audio)]
    [TestCase(ActionType.ExecuteScript)]
    [TestCase(ActionType.Matrix)]
    public void MissingOrUnsupportedPayload_IsRejected(ActionType type)
    {
        Assert.That(Validate(new Workflow { Actions = [new WorkflowAction { Type = type }] }).IsValid, Is.False);
    }

    [Test]
    public void DuplicateWorkflowIds_AreRejected()
    {
        var first = new Workflow { Actions = [Command()] };
        var second = new Workflow { Id = first.Id, Actions = [Command()] };
        var result = new WorkflowValidator().Validate(new Project { Workflows = [first, second] });
        Assert.That(result.Issues.Select(issue => issue.Code), Does.Contain(WorkflowValidationCodes.DuplicateWorkflowId));
    }
}
