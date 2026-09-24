// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;
using System.Text.Json;

[TestFixture]
internal sealed class WorkflowActionSequenceSerializationTests
{
    [Test]
    public void RoundTripPreservesListOrderIdsAndTypedSettings()
    {
        var first = new WorkflowAction { Name = "Gong", Number = 9, Type = ActionType.Audio, Audio = new() { FilePath = "gong.wav" }, DelayAfterMs = 200 };
        var second = new WorkflowAction { Name = "Arrival", Number = 1, Type = ActionType.Announcement, Announcement = new() { Message = "{StationName}", VoiceName = "test-voice" } };
        var workflow = new Workflow { Name = "Arrival", Description = "Reusable", Actions = [first, second] };
        var json = JsonSerializer.Serialize(workflow, JsonOptions.Default);
        var restored = JsonSerializer.Deserialize<Workflow>(json, JsonOptions.Default)!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored.Id, Is.EqualTo(workflow.Id));
            Assert.That(restored.Description, Is.EqualTo("Reusable"));
            Assert.That(restored.Actions.Select(action => action.Id), Is.EqualTo(new[] { first.Id, second.Id }));
            Assert.That(restored.Actions[0].Audio!.FilePath, Is.EqualTo("gong.wav"));
            Assert.That(restored.Actions[0].DelayAfterMs, Is.EqualTo(200));
            Assert.That(restored.Actions[1].Announcement!.Message, Is.EqualTo("{StationName}"));
            Assert.That(json, Does.Contain("\"actions\""));
            Assert.That(json, Does.Not.Contain("entryStepId").And.Not.Contain("nextStepId").And.Not.Contain("steps"));
        }
    }

    [Test]
    public void PreviousGraphPreservesIdentityButDoesNotGuessAnActionOrder()
    {
        var id = Guid.NewGuid();
        var json = $$"""{"id":"{{id}}","name":"Rebuild me","entryStepId":"{{Guid.NewGuid()}}","steps":[{"kind":"parallel","branches":[]}]}""";
        var workflow = JsonSerializer.Deserialize<Workflow>(json, JsonOptions.Default)!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflow.Id, Is.EqualTo(id));
            Assert.That(workflow.Name, Is.EqualTo("Rebuild me"));
            Assert.That(workflow.Actions, Is.Empty);
        }
    }
}
