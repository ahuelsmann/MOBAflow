// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;

using System.Text.Json;

[TestFixture]
internal class WorkflowTests
{
    [Test]
    public void Constructor_InitializesDefaults()
    {
        var workflow = new Workflow();

        Assert.That(workflow.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(workflow.Name, Is.EqualTo("New Flow"));
        Assert.That(workflow.Description, Is.EqualTo(string.Empty));
        Assert.That(workflow.Actions, Is.Not.Null);
        Assert.That(workflow.Actions, Is.Empty);
    }

    [Test]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var actions = new List<WorkflowAction> { new() };

        var workflow = new Workflow
        {
            Id = id,
            Name = "Arrival Workflow",
            Description = "Announcement on arrival",
            Actions = actions,
        };

        Assert.That(workflow.Id, Is.EqualTo(id));
        Assert.That(workflow.Name, Is.EqualTo("Arrival Workflow"));
        Assert.That(workflow.Description, Is.EqualTo("Announcement on arrival"));
        Assert.That(workflow.Actions, Is.SameAs(actions));
    }

    [Test]
    public void Actions_CanAddAndRemove()
    {
        var workflow = new Workflow();
        var action = new WorkflowAction
        {
            Name = "Play Gong",
            Type = ActionType.Audio
        };

        workflow.Actions.Add(action);
        Assert.That(workflow.Actions, Has.Count.EqualTo(1));
        Assert.That(workflow.Actions[0].Name, Is.EqualTo("Play Gong"));

        workflow.Actions.Remove(action);
        Assert.That(workflow.Actions, Is.Empty);
    }

    [Test]
    public void Actions_WithAnnouncementPayload_WorkCorrectly()
    {
        var workflow = new Workflow();
        var action = new WorkflowAction
        {
            Name = "Announcement",
            Type = ActionType.Announcement,
            Number = 1,
            DelayAfterMs = 500,
            Announcement = new AnnouncementActionPayload
            {
                Message = "Zug fährt ab",
                VoiceName = "de-DE-KatjaNeural"
            }
        };

        workflow.Actions.Add(action);

        Assert.That(workflow.Actions, Has.Count.EqualTo(1));
        Assert.That(workflow.Actions[0].Type, Is.EqualTo(ActionType.Announcement));
        Assert.That(workflow.Actions[0].Number, Is.EqualTo(1u));
        Assert.That(workflow.Actions[0].DelayAfterMs, Is.EqualTo(500));
        Assert.That(workflow.Actions[0].Announcement, Is.Not.Null);
        Assert.That(workflow.Actions[0].Announcement!.Message, Is.EqualTo("Zug fährt ab"));
    }

    [Test]
    public void WorkflowAction_LegacyParametersJson_DoesNotCreateTypedPayload()
    {
        const string json = """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "name": "Gong",
              "number": 1,
              "type": 2,
              "delayAfterMs": 100,
              "parameters": { "FilePath": "C:\\sounds\\gong.wav" }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json, JsonOptions.Default);
        Assert.That(action, Is.Not.Null);
        Assert.That(action!.Type, Is.EqualTo(ActionType.Command));
        Assert.That(action.DelayAfterMs, Is.EqualTo(100));
        Assert.That(action.Audio, Is.Null);
    }
}
