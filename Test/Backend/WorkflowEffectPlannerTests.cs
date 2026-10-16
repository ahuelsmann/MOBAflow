// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Domain;
using Moba.Domain.Enum;
using System.Text.Json;

[TestFixture]
internal sealed class WorkflowEffectPlannerTests
{
    private readonly WorkflowEffectPlanner _planner = new();

    [TestCase(ActionType.Command)]
    [TestCase(ActionType.Audio)]
    [TestCase(ActionType.Announcement)]
    [TestCase(ActionType.ExecuteScript)]
    [TestCase(ActionType.SelectSignalAspect)]
    [TestCase(ActionType.TrainDestinationDisplay)]
    [TestCase(ActionType.ChangeJourneyStop)]
    public void Plan_MissingPayload_HasNoExecutableRequest(ActionType type)
    {
        var result = _planner.Plan(new WorkflowAction { Type = type });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Request, Is.Null);
            Assert.That(result.Effect, Is.Null);
        }
    }

    [Test]
    public void Plan_ScriptRequest_RemainsPrivateWhenPlanIsSerializedOrFormatted()
    {
        var action = new WorkflowAction
        {
            Type = ActionType.ExecuteScript,
            PowerShell = new() { ScriptPath = "private-marker/script.ps1", Arguments = "argument-marker" }
        };
        var result = _planner.Plan(action);
        var request = (WorkflowScriptEffectRequest)result.Request!;
        var json = JsonSerializer.Serialize(result);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.ScriptPath, Is.EqualTo(action.PowerShell.ScriptPath));
            Assert.That(request.Arguments, Is.EqualTo(action.PowerShell.Arguments));
            Assert.That(json, Does.Not.Contain("private-marker").And.Not.Contain("argument-marker").And.Not.Contain("Request"));
            Assert.That(request.ToString(), Does.Not.Contain("private-marker").And.Not.Contain("argument-marker"));
        }
    }

    [Test]
    public void Plan_InvalidSignalReference_HasNoExecutableRequest()
    {
        var result = _planner.Plan(new WorkflowAction
        {
            Type = ActionType.SelectSignalAspect,
            SelectSignalAspect = new() { BaseAddress = 201, MultiplexerArticleNumber = "unknown" }
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Request, Is.Null);
        }
    }

    [TestCaseSource(nameof(SupportedActions))]
    public void Plan_SupportedAction_ReturnsSanitizedEffect(
        WorkflowAction action,
        WorkflowEffectCategory expectedCategory,
        string expectedResourceKey)
    {
        // Act
        var result = _planner.Plan(action);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Effect, Is.Not.Null);
            Assert.That(result.Effect!.Category, Is.EqualTo(expectedCategory));
            Assert.That(result.Effect.Resources, Has.Count.EqualTo(1));
            Assert.That(result.Effect.Resources[0].Key, Is.EqualTo(expectedResourceKey));
            Assert.That(result.Effect.Resources[0].Access, Is.EqualTo(WorkflowResourceAccess.ExclusiveWrite));
            if (action.Type == ActionType.ChangeJourneyStop)
                Assert.That(result.Request, Is.Null, "Stop transitions stay in shared domain logic.");
            else if (action.Type == ActionType.Announcement)
                Assert.That(result.Request, Is.Null, "Announcement requests need the invocation's station context.");
            else
                Assert.That(result.Request!.ActionId, Is.EqualTo(action.Id));
        });
    }

    [Test]
    public void Plan_InvalidTypedPayload_ReturnsFieldSpecificIssueWithoutEffect()
    {
        // Arrange
        var action = new WorkflowAction
        {
            Type = ActionType.ChangeJourneyStop,
            ChangeJourneyStop = new ChangeJourneyStopActionPayload
            {
                MoveToNextStop = false,
                TargetStationId = null
            }
        };

        // Act
        var result = _planner.Plan(action);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Effect, Is.Null);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].FieldPath, Is.EqualTo("changeJourneyStop.targetStationId"));
        });
    }

    [Test]
    public void Plan_UnsupportedAction_ReturnsValidationIssueWithoutInvokingEffects()
    {
        // Act
        var result = _planner.Plan(new WorkflowAction { Type = ActionType.Matrix });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Effect, Is.Null);
            Assert.That(result.Issues[0].FieldPath, Is.EqualTo("type"));
        });
    }

    private static IEnumerable<TestCaseData> SupportedActions()
    {
        var displayId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        yield return new TestCaseData(
            new WorkflowAction
            {
                Type = ActionType.Command,
                Command = new CommandActionPayload { BytesBase64 = "AQID" }
            },
            WorkflowEffectCategory.CommandStation,
            "z21:command-station");
        yield return new TestCaseData(
            new WorkflowAction
            {
                Type = ActionType.Audio,
                Audio = new AudioActionPayload { FilePath = "gong.wav" }
            },
            WorkflowEffectCategory.AudioOutput,
            "audio-output:default");
        yield return new TestCaseData(
            new WorkflowAction
            {
                Type = ActionType.Announcement,
                Announcement = new AnnouncementActionPayload()
            },
            WorkflowEffectCategory.SpeechOutput,
            "audio-output:default");
        yield return new TestCaseData(
            new WorkflowAction
            {
                Type = ActionType.ExecuteScript,
                PowerShell = new PowerShellActionPayload { ScriptPath = "update.ps1" }
            },
            WorkflowEffectCategory.ScriptProcess,
            "process:powershell");
        yield return new TestCaseData(
            new WorkflowAction
            {
                Type = ActionType.SelectSignalAspect,
                SelectSignalAspect = new SelectSignalAspectActionPayload
                {
                    BaseAddress = 201,
                    MultiplexerArticleNumber = "5229",
                    SignalArticleNumber = "4046"
                }
            },
            WorkflowEffectCategory.Signal,
            "z21:turnout:201:0");
        yield return new TestCaseData(
            new WorkflowAction
            {
                Type = ActionType.TrainDestinationDisplay,
                TrainDestinationDisplay = new TrainDestinationDisplayActionPayload { DisplayDeviceId = displayId }
            },
            WorkflowEffectCategory.Display,
            $"display:{displayId:D}");
        yield return new TestCaseData(
            new WorkflowAction
            {
                Type = ActionType.ChangeJourneyStop,
                ChangeJourneyStop = new ChangeJourneyStopActionPayload { MoveToNextStop = true }
            },
            WorkflowEffectCategory.JourneyState,
            "journey:current");
    }
}
