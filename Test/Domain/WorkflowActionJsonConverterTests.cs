// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using System.Text.Json;

using Moba.Domain;
using Moba.Domain.Enum;

[TestFixture]
internal sealed class WorkflowActionJsonConverterTests
{
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
    }}
