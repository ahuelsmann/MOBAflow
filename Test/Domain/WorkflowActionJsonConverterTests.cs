// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;
using System.Text.Json;

[TestFixture]
internal sealed class WorkflowActionJsonConverterTests
{
    [Test]
    public void Deserialize_NullToken_ReturnsNull()
    {
        var action = JsonSerializer.Deserialize<WorkflowAction>("null");

        Assert.That(action, Is.Null);
    }

    [Test]
    public void Deserialize_CaseInsensitiveMetadata_PreservesEveryValue()
    {
        // Arrange
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var json = $$"""
            {
              "ID": "{{id}}",
              "NAME": "Play horn",
              "NUMBER": 42,
              "TYPE": "Audio",
              "DELAYAFTERMS": 125,
              "AUDIO": { "FILEPATH": "sounds/horn.wav" }
            }
            """;

        // Act
        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action!.Id, Is.EqualTo(id));
            Assert.That(action.Name, Is.EqualTo("Play horn"));
            Assert.That(action.Number, Is.EqualTo(42));
            Assert.That(action.Type, Is.EqualTo(ActionType.Audio));
            Assert.That(action.DelayAfterMs, Is.EqualTo(125));
            Assert.That(action.Audio?.FilePath, Is.EqualTo("sounds/horn.wav"));
        });
    }

    [Test]
    public void Deserialize_InvalidMetadata_UsesSafeDefaults()
    {
        const string json = """
            {
              "id": "not-a-guid",
              "name": 12,
              "number": -1,
              "type": "unknown",
              "delayAfterMs": "later"
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(action.Name, Is.Empty);
            Assert.That(action.Number, Is.Zero);
            Assert.That(action.Type, Is.EqualTo(ActionType.Command));
            Assert.That(action.DelayAfterMs, Is.Zero);
        });
    }

    [Test]
    public void Serialize_NullAction_WritesNullToken()
    {
        var json = JsonSerializer.Serialize<WorkflowAction?>(null);

        Assert.That(json, Is.EqualTo("null"));
    }

    [TestCase(ActionType.Command, "command")]
    [TestCase(ActionType.Audio, "audio")]
    [TestCase(ActionType.Announcement, "announcement")]
    [TestCase(ActionType.ExecuteScript, "powerShell")]
    [TestCase(ActionType.SelectSignalAspect, "selectSignalAspect")]
    [TestCase(ActionType.TrainDestinationDisplay, "trainDestinationDisplay")]
    [TestCase(ActionType.ChangeJourneyStop, "changeJourneyStop")]
    public void Serialize_DeclaredTypeWithoutPayload_WritesDefaultPayload(ActionType type, string payloadProperty)
    {
        // Arrange
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var action = new WorkflowAction
        {
            Id = id,
            Name = "Configured action",
            Number = 7,
            Type = type,
            DelayAfterMs = 250
        };

        // Act
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(action));
        var root = document.RootElement;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("id").GetGuid(), Is.EqualTo(id));
            Assert.That(root.GetProperty("name").GetString(), Is.EqualTo("Configured action"));
            Assert.That(root.GetProperty("number").GetUInt32(), Is.EqualTo(7));
            Assert.That(root.GetProperty("type").GetInt32(), Is.EqualTo((int)type));
            Assert.That(root.GetProperty("delayAfterMs").GetInt32(), Is.EqualTo(250));
            Assert.That(root.GetProperty(payloadProperty).ValueKind, Is.EqualTo(JsonValueKind.Object));
        });
    }

    [Test]
    public void Serialize_TypeWithoutDescriptor_WritesEveryPresentPayloadOnly()
    {
        // Arrange
        var action = new WorkflowAction
        {
            Type = ActionType.Matrix,
            Command = new CommandActionPayload { Address = 3 },
            Audio = new AudioActionPayload { FilePath = "sounds/horn.wav" }
        };

        // Act
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(action));
        var root = document.RootElement;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("command").GetProperty("address").GetInt32(), Is.EqualTo(3));
            Assert.That(root.GetProperty("audio").GetProperty("filePath").GetString(), Is.EqualTo("sounds/horn.wav"));
            Assert.That(root.TryGetProperty("announcement", out _), Is.False);
        });
    }

    [Test]
    public void Deserialize_NonObjectPayloadAndLegacyParameters_AreIgnored()
    {
        const string json = """
            {
              "type": 1,
              "audio": "sounds/horn.wav",
              "parameters": []
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Audio, Is.Null);
    }

    [Test]
    public void RoundTrip_ChangeJourneyStopPayload_PreservesConfiguredTarget()
    {
        var targetId = Guid.NewGuid();
        var action = new WorkflowAction
        {
            Type = ActionType.ChangeJourneyStop,
            ChangeJourneyStop = new ChangeJourneyStopActionPayload
            {
                MoveToNextStop = false,
                TargetStationId = targetId
            }
        };

        var roundTripped = JsonSerializer.Deserialize<WorkflowAction>(JsonSerializer.Serialize(action));

        Assert.That(roundTripped?.ChangeJourneyStop?.TargetStationId, Is.EqualTo(targetId));
        Assert.That(roundTripped?.ChangeJourneyStop?.MoveToNextStop, Is.False);
    }

    [Test]
    public void SerializeDeserialize_Should_RoundTripTrainDestinationDisplayPayload()
    {
        var displayDeviceId = Guid.NewGuid();
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Name = "Update display",
            Number = 3,
            Type = ActionType.TrainDestinationDisplay,
            DelayAfterMs = 250,
            TrainDestinationDisplay = new TrainDestinationDisplayActionPayload
            {
                DisplayDeviceId = displayDeviceId,
                ClearBeforeRender = false
            }
        };

        var json = JsonSerializer.Serialize(action);
        var roundTripped = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.Type, Is.EqualTo(ActionType.TrainDestinationDisplay));
        Assert.That(roundTripped.TrainDestinationDisplay, Is.Not.Null);
        Assert.That(roundTripped.TrainDestinationDisplay!.DisplayDeviceId, Is.EqualTo(displayDeviceId));
        Assert.That(roundTripped.TrainDestinationDisplay.ClearBeforeRender, Is.False);
    }

    [Test]
    public void TryGetDisplayDeviceId_Should_ReturnConfiguredDeviceId()
    {
        var displayDeviceId = Guid.NewGuid();
        var action = new WorkflowAction
        {
            Type = ActionType.TrainDestinationDisplay,
            TrainDestinationDisplay = new TrainDestinationDisplayActionPayload
            {
                DisplayDeviceId = displayDeviceId
            }
        };

        var result = WorkflowActionParameterBinding.TryGetDisplayDeviceId(action, out var actual);

        Assert.That(result, Is.True);
        Assert.That(actual, Is.EqualTo(displayDeviceId));
    }

    [Test]
    public void SerializeDeserialize_Should_RoundTripSelectSignalAspectPayload()
    {
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Name = "Set signal",
            Number = 4,
            Type = ActionType.SelectSignalAspect,
            DelayAfterMs = 100,
            SelectSignalAspect = new SelectSignalAspectActionPayload
            {
                BaseAddress = 201,
                SignalAspect = SignalAspect.Hp0,
                MultiplexerArticleNumber = "5229",
                SignalArticleNumber = "4046"
            }
        };

        var json = JsonSerializer.Serialize(action);
        var roundTripped = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.Type, Is.EqualTo(ActionType.SelectSignalAspect));
        Assert.That(roundTripped.SelectSignalAspect, Is.Not.Null);
        Assert.That(roundTripped.SelectSignalAspect!.BaseAddress, Is.EqualTo(201));
        Assert.That(roundTripped.SelectSignalAspect.SignalAspect, Is.EqualTo(SignalAspect.Hp0));
        Assert.That(roundTripped.SelectSignalAspect.MultiplexerArticleNumber, Is.EqualTo("5229"));
        Assert.That(roundTripped.SelectSignalAspect.SignalArticleNumber, Is.EqualTo("4046"));
    }

    [Test]
    public void SerializeDeserialize_Should_RoundTripPowerShellPayload()
    {
        var action = new WorkflowAction
        {
            Id = Guid.NewGuid(),
            Name = "Run Script",
            Number = 5,
            Type = ActionType.ExecuteScript,
            DelayAfterMs = 150,
            PowerShell = new PowerShellActionPayload
            {
                ScriptPath = "scripts/update.ps1",
                Arguments = "-Verbose"
            }
        };

        var json = JsonSerializer.Serialize(action);
        var roundTripped = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.Type, Is.EqualTo(ActionType.ExecuteScript));
        Assert.That(roundTripped.PowerShell, Is.Not.Null);
        Assert.That(roundTripped.PowerShell!.ScriptPath, Is.EqualTo("scripts/update.ps1"));
        Assert.That(roundTripped.PowerShell.Arguments, Is.EqualTo("-Verbose"));
    }
}
