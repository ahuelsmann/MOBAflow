// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using System.Text.Json;

using Moba.Domain;
using Moba.Domain.Enum;

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
