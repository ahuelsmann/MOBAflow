// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Common.Events;
using global::Moba.Domain;
using Microsoft.Extensions.Logging.Abstractions;

internal sealed class LocomotiveWhistleAutomationServiceTests
{
    [Test]
    public async Task HandleFeedbackAsync_SendsDelayedMomentaryFunction()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gateway = new RecordingGateway();
        using var service = new LocomotiveWhistleAutomationService(
            eventBus, gateway, NullLogger<LocomotiveWhistleAutomationService>.Instance);
        var locomotive = new Locomotive { DigitalAddress = 12 };
        service.Activate(new Project
        {
            Locomotives = [locomotive],
            LocomotiveWhistleRules =
            [
                new LocomotiveWhistleRule
                {
                    LocomotiveId = locomotive.Id,
                    InPort = 7,
                    FunctionIndex = 31,
                    DelayMilliseconds = 1,
                    ActiveDurationMilliseconds = 1
                }
            ]
        });

        await service.HandleFeedbackAsync(7);

        Assert.That(gateway.Commands, Is.EqualTo(new[] { (12, 31, true), (12, 31, false) }));
    }

    [Test]
    public async Task ActivateNewProject_CancelsPendingRuleWithoutTurningItOn()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gateway = new RecordingGateway();
        using var service = new LocomotiveWhistleAutomationService(
            eventBus, gateway, NullLogger<LocomotiveWhistleAutomationService>.Instance);
        var locomotive = new Locomotive { DigitalAddress = 3 };
        service.Activate(new Project
        {
            Locomotives = [locomotive],
            LocomotiveWhistleRules = [new LocomotiveWhistleRule { LocomotiveId = locomotive.Id, InPort = 1, DelayMilliseconds = 200, ActiveDurationMilliseconds = 20 }]
        });

        var pending = service.HandleFeedbackAsync(1);
        service.Activate(new Project());
        await pending;

        Assert.That(gateway.Commands, Is.Empty);
    }

    [Test]
    public void Validate_EnforcesFunctionAndTimingBoundaries()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        using var service = new LocomotiveWhistleAutomationService(
            eventBus, new RecordingGateway(), NullLogger<LocomotiveWhistleAutomationService>.Instance);

        var errors = service.Validate(new LocomotiveWhistleRule
        {
            LocomotiveId = Guid.Empty,
            InPort = 0,
            FunctionIndex = 32,
            DelayMilliseconds = -1,
            ActiveDurationMilliseconds = 0
        });

        Assert.That(errors, Has.Count.EqualTo(5));
    }

    private sealed class RecordingGateway : ILocomotiveFunctionCommandGateway
    {
        public List<(int Address, int Function, bool IsOn)> Commands { get; } = [];
        public bool IsConnected { get; set; } = true;

        public Task SetFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
        {
            Commands.Add((address, functionIndex, isOn));
            return Task.CompletedTask;
        }
    }
}
