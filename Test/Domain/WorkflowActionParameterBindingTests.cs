// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;
using Moba.Domain.Enum;

/// <summary>
/// Tests for <see cref="WorkflowActionParameterBinding"/> typed payload readers used by workflow handlers.
/// </summary>
[TestFixture]
internal sealed class WorkflowActionParameterBindingTests
{
    [Test]
    public void TryGetCommandBytes_ValidBase64_ReturnsDecodedBytes()
    {
        var action = new WorkflowAction
        {
            Type = ActionType.Command,
            Command = new CommandActionPayload { BytesBase64 = Convert.ToBase64String([0x04, 0x00, 0x85, 0x00]) }
        };

        var ok = WorkflowActionParameterBinding.TryGetCommandBytes(action, out var bytes);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(bytes, Is.EqualTo(new byte[] { 0x04, 0x00, 0x85, 0x00 }));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-base64!!!")]
    public void TryGetCommandBytes_InvalidOrMissingBase64_ReturnsFalse(string? value)
    {
        var action = new WorkflowAction
        {
            Type = ActionType.Command,
            Command = new CommandActionPayload { BytesBase64 = value }
        };

        var ok = WorkflowActionParameterBinding.TryGetCommandBytes(action, out var bytes);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(bytes, Is.Null);
        });
    }

    [Test]
    public void TryGetAudioFilePath_WithPath_ReturnsTrimmedValue()
    {
        var action = new WorkflowAction
        {
            Type = ActionType.Audio,
            Audio = new AudioActionPayload { FilePath = " sounds/horn.wav " }
        };

        var ok = WorkflowActionParameterBinding.TryGetAudioFilePath(action, out var path);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(path, Is.EqualTo(" sounds/horn.wav "));
        });
    }

    [Test]
    public void TryGetDisplayDeviceId_EmptyGuid_ReturnsFalse()
    {
        var action = new WorkflowAction
        {
            Type = ActionType.TrainDestinationDisplay,
            TrainDestinationDisplay = new TrainDestinationDisplayActionPayload()
        };

        var ok = WorkflowActionParameterBinding.TryGetDisplayDeviceId(action, out var deviceId);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(deviceId, Is.EqualTo(Guid.Empty));
        });
    }
}
