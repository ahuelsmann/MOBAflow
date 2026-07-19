// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;
using System.Text.Json;

/// <summary>
/// Tests for legacy <c>parameters</c> migration into typed workflow action payloads.
/// These guard backward compatibility when loading older solution.json workflow definitions.
/// </summary>
[TestFixture]
internal sealed class WorkflowActionLegacyParameterMigratorTests
{
    [Test]
    public void Deserialize_LegacyCommandParameters_MergesIntoCommandPayload()
    {
        const string json = """
            {
              "type": 2,
              "name": "Drive loco",
              "number": 1,
              "parameters": {
                "Address": 3,
                "Speed": 50,
                "Direction": "forward",
                "Bytes": "AQIDBA=="
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action!.Type, Is.EqualTo(ActionType.Command));
            Assert.That(action.Command, Is.Not.Null);
            Assert.That(action.Command!.Address, Is.EqualTo(3));
            Assert.That(action.Command.Speed, Is.EqualTo(50));
            Assert.That(action.Command.Direction, Is.EqualTo("forward"));
            Assert.That(action.Command.BytesBase64, Is.EqualTo("AQIDBA=="));
        });
    }

    [Test]
    public void Deserialize_LegacyCommandWithByteArray_DecodesBytesBase64()
    {
        const string json = """
            {
              "type": 2,
              "parameters": {
                "Bytes": [1, 2, 3]
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Command?.BytesBase64, Is.EqualTo(Convert.ToBase64String([1, 2, 3])));
    }

    [Test]
    public void Deserialize_LegacyCommandByteArray_KeepsBoundaryBytesAndIgnoresInvalidItems()
    {
        const string json = """
            {
              "type": 2,
              "parameters": {
                "Bytes": [0, -1, 255, 256, "invalid"]
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Command?.BytesBase64, Is.EqualTo(Convert.ToBase64String([0, 255])));
    }

    [TestCase("\"not-base64\"")]
    [TestCase("[]")]
    [TestCase("[256]")]
    public void Deserialize_LegacyCommandInvalidBytes_LeavesBytesUnset(string bytesJson)
    {
        var json = $$"""
            {
              "type": 2,
              "parameters": {
                "Bytes": {{bytesJson}}
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Command?.BytesBase64, Is.Null);
    }

    [Test]
    public void Deserialize_LegacyCommandBytes_FillsExistingEmptyPayload()
    {
        const string json = """
            {
              "type": 2,
              "command": { "bytesBase64": "" },
              "parameters": { "Bytes": [1, 2, 3] }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Command?.BytesBase64, Is.EqualTo(Convert.ToBase64String([1, 2, 3])));
    }

    [Test]
    public void Deserialize_LegacyCommandBytes_DoesNotOverwriteExistingBytes()
    {
        const string json = """
            {
              "type": 2,
              "command": { "bytesBase64": "CQgH" },
              "parameters": { "Bytes": [1, 2, 3] }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Command?.BytesBase64, Is.EqualTo("CQgH"));
    }

    [Test]
    public void Deserialize_LegacyCommandWithFilePath_UpgradesToAudioAction()
    {
        const string json = """
            {
              "type": 2,
              "parameters": {
                "FilePath": "sounds/horn.wav"
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action!.Type, Is.EqualTo(ActionType.Audio));
            Assert.That(action.Audio?.FilePath, Is.EqualTo("sounds/horn.wav"));
        });
    }

    [Test]
    public void Deserialize_LegacyAudioFileAlias_MergesIntoAudioPayload()
    {
        const string json = """
            {
              "type": 1,
              "parameters": {
                "AudioFile": "announcements/station.mp3"
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Audio?.FilePath, Is.EqualTo("announcements/station.mp3"));
    }

    [Test]
    public void Deserialize_LegacyAudio_NonStringFilePathFallsBackToAudioFileAlias()
    {
        const string json = """
            {
              "type": 1,
              "parameters": {
                "FilePath": 42,
                "AudioFile": "announcements/fallback.mp3"
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Audio?.FilePath, Is.EqualTo("announcements/fallback.mp3"));
    }

    [Test]
    public void Deserialize_LegacyAudio_NonStringAudioFileLeavesPathUnset()
    {
        const string json = """
            {
              "type": 1,
              "parameters": { "AudioFile": 42 }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Audio?.FilePath, Is.Null);
    }

    [Test]
    public void Deserialize_LegacyAudioParameters_DoNotOverwriteExistingFilePath()
    {
        const string json = """
            {
              "type": 1,
              "audio": { "filePath": "current.wav" },
              "parameters": { "FilePath": "legacy.wav" }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Audio?.FilePath, Is.EqualTo("current.wav"));
    }

    [Test]
    public void Deserialize_LegacyTrainDestinationDisplayFalseFlag_PreservesFalse()
    {
        const string json = """
            {
              "type": 6,
              "parameters": { "ClearBeforeRender": false }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.TrainDestinationDisplay?.ClearBeforeRender, Is.False);
    }

    [Test]
    public void Deserialize_LegacyAnnouncementParameters_MergesMessageVoiceAndRate()
    {
        const string json = """
            {
              "type": 0,
              "parameters": {
                "Message": "Naechster Halt",
                "VoiceName": "de-DE-KatjaNeural",
                "Rate": -2
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action!.Type, Is.EqualTo(ActionType.Announcement));
            Assert.That(action.Announcement?.Message, Is.EqualTo("Naechster Halt"));
            Assert.That(action.Announcement?.VoiceName, Is.EqualTo("de-DE-KatjaNeural"));
            Assert.That(action.Announcement?.Rate, Is.EqualTo(-2));
        });
    }

    [Test]
    public void Deserialize_LegacyAnnouncementParameters_PreserveExistingPayloadInstanceValues()
    {
        const string json = """
            {
              "type": 0,
              "announcement": { "message": "Current message" },
              "parameters": { "VoiceName": "Legacy voice" }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action?.Announcement?.Message, Is.EqualTo("Current message"));
            Assert.That(action?.Announcement?.VoiceName, Is.EqualTo("Legacy voice"));
        });
    }

    [Test]
    public void Deserialize_LegacyPowerShellParameters_MergesScriptPathAndArguments()
    {
        const string json = """
            {
              "type": 3,
              "parameters": {
                "ScriptPath": "scripts/run.ps1",
                "Arguments": "-WhatIf"
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action!.PowerShell?.ScriptPath, Is.EqualTo("scripts/run.ps1"));
            Assert.That(action.PowerShell?.Arguments, Is.EqualTo("-WhatIf"));
        });
    }

    [Test]
    public void Deserialize_LegacyPowerShellParameters_PreserveExistingPayloadInstanceValues()
    {
        const string json = """
            {
              "type": 3,
              "powerShell": { "scriptPath": "scripts/current.ps1" },
              "parameters": { "Arguments": "-Legacy" }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action?.PowerShell?.ScriptPath, Is.EqualTo("scripts/current.ps1"));
            Assert.That(action?.PowerShell?.Arguments, Is.EqualTo("-Legacy"));
        });
    }

    [Test]
    public void Deserialize_LegacyTrainDestinationDisplayParameters_MergesDeviceAndClearFlag()
    {
        var deviceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var json = $$"""
            {
              "type": 6,
              "parameters": {
                "DisplayDeviceId": "{{deviceId}}",
                "ClearBeforeRender": true
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action!.TrainDestinationDisplay?.DisplayDeviceId, Is.EqualTo(deviceId));
            Assert.That(action.TrainDestinationDisplay?.ClearBeforeRender, Is.True);
        });
    }

    [Test]
    public void Deserialize_LegacyTrainDestinationDisplay_PreservesExistingDeviceId()
    {
        var currentDeviceId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var json = $$"""
            {
              "type": 6,
              "trainDestinationDisplay": {
                "displayDeviceId": "{{currentDeviceId}}",
                "clearBeforeRender": true
              },
              "parameters": { "ClearBeforeRender": false }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action?.TrainDestinationDisplay?.DisplayDeviceId, Is.EqualTo(currentDeviceId));
            Assert.That(action?.TrainDestinationDisplay?.ClearBeforeRender, Is.False);
        });
    }

    [Test]
    public void Deserialize_LegacyTrainDestinationDisplay_NonBooleanClearFlagIsIgnored()
    {
        const string json = """
            {
              "type": 6,
              "parameters": { "ClearBeforeRender": "false" }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.TrainDestinationDisplay?.ClearBeforeRender, Is.True);
    }

    [TestCase(ActionType.SelectSignalAspect)]
    [TestCase(ActionType.ChangeJourneyStop)]
    public void Deserialize_ActionWithoutLegacyMigration_IgnoresLegacyParameters(ActionType actionType)
    {
        var json = $$"""
            {
              "type": {{(int)actionType}},
              "parameters": { "unsupported": "value" }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.That(action?.Type, Is.EqualTo(actionType));
    }

    [Test]
    public void Deserialize_LegacyParameters_DoNotOverwriteExistingTypedPayload()
    {
        const string json = """
            {
              "type": 2,
              "command": {
                "address": 99,
                "speed": 10
              },
              "parameters": {
                "Address": 3,
                "Speed": 50
              }
            }
            """;

        var action = JsonSerializer.Deserialize<WorkflowAction>(json);

        Assert.Multiple(() =>
        {
            Assert.That(action!.Command?.Address, Is.EqualTo(99));
            Assert.That(action.Command?.Speed, Is.EqualTo(10));
        });
    }
}
