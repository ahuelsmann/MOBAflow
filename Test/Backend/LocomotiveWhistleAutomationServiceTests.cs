// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Common.Events;
using global::Moba.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moq;

internal sealed class LocomotiveWhistleAutomationServiceTests
{
    [Test]
    public async Task CounterFilteredActivation_StillReachesRawWhistleConsumer()
    {
        var z21 = new Mock<IZ21>();
        using var counters = new InPortCounterService(z21.Object, new AppSettings
        {
            Counter = { CountOfFeedbackPoints = 1, UseTimerFilter = true, TimerIntervalSeconds = 3600 }
        });
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var onCalls = 0;
        var offCalls = 0;
        var offs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateway = new Mock<ILocomotiveFunctionCommandGateway>();
        gateway.SetupGet(value => value.IsConnected).Returns(true);
        gateway.Setup(value => value.SetFunctionAsync(12, 2, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns<int, int, bool, CancellationToken>((_, _, on, _) =>
            {
                if (on) Interlocked.Increment(ref onCalls);
                else if (Interlocked.Increment(ref offCalls) == 1) offs.TrySetResult();
                return Task.CompletedTask;
            });
        using var whistles = new LocomotiveWhistleAutomationService(bus, gateway.Object,
            NullLogger<LocomotiveWhistleAutomationService>.Instance);
        var locomotive = new Locomotive { DigitalAddress = 12 };
        whistles.Activate(new Project
        {
            Locomotives = [locomotive],
            LocomotiveWhistleRules = [new() { LocomotiveId = locomotive.Id, InPort = 1, FunctionIndex = 2, ActiveDurationMilliseconds = 1 }]
        });
        InPortCounterServiceTests.Raise(z21, 1);
        InPortCounterServiceTests.Raise(z21, 1);
        bus.Publish(new FeedbackReceivedEvent(1));
        await offs.Task.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(counters.GetSnapshot().Single().Count, Is.EqualTo(1UL));
            Assert.That(onCalls, Is.EqualTo(1));
            Assert.That(offCalls, Is.EqualTo(1));
        }
    }

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
    public async Task HandleFeedbackAsync_CoalescesRulesForSameLocomotiveFunction()
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
                    InPort = 1,
                    FunctionIndex = 2,
                    ActiveDurationMilliseconds = 200
                },
                new LocomotiveWhistleRule
                {
                    LocomotiveId = locomotive.Id,
                    InPort = 2,
                    FunctionIndex = 2,
                    ActiveDurationMilliseconds = 20
                }
            ]
        });

        var longPulse = service.HandleFeedbackAsync(1);
        await gateway.FunctionOn.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var shortPulse = service.HandleFeedbackAsync(2);
        await Task.Delay(75);

        Assert.That(gateway.Commands, Is.EqualTo(new[] { (12, 2, true) }));

        await Task.WhenAll(longPulse, shortPulse);
        Assert.That(gateway.Commands, Is.EqualTo(new[] { (12, 2, true), (12, 2, false) }));
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

    [Test]
    public async Task SavedDefinitions_AreIsolatedFromEditorChanges_AndKeepExecutingWhistles()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var gateway = new RecordingGateway();
        using var service = new LocomotiveWhistleAutomationService(
            eventBus, gateway, NullLogger<LocomotiveWhistleAutomationService>.Instance);
        var locomotive = new Locomotive { DigitalAddress = 12 };
        var rule = new LocomotiveWhistleRule
        {
            LocomotiveId = locomotive.Id, InPort = 7, FunctionIndex = 2, ActiveDurationMilliseconds = 20
        };
        var project = new Project { Locomotives = [locomotive], LocomotiveWhistleRules = [rule] };
        service.Activate(project);
        locomotive.DigitalAddress = 24;
        rule.FunctionIndex = 31;

        var running = service.HandleFeedbackAsync(7);
        await gateway.FunctionOn.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        service.UpdateDefinitions(project);
        await running.ConfigureAwait(false);

        Assert.That(gateway.Commands, Is.EqualTo(new[] { (12, 2, true), (12, 2, false) }));
        await service.HandleFeedbackAsync(7).ConfigureAwait(false);
        Assert.That(gateway.Commands, Is.EqualTo(new[] { (12, 2, true), (12, 2, false), (24, 31, true), (24, 31, false) }));
    }

    private sealed class RecordingGateway : ILocomotiveFunctionCommandGateway
    {
        public List<(int Address, int Function, bool IsOn)> Commands { get; } = [];
        public TaskCompletionSource FunctionOn { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsConnected { get; set; } = true;

        public Task SetFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
        {
            Commands.Add((address, functionIndex, isOn));
            if (isOn)
            {
                FunctionOn.TrySetResult();
            }
            return Task.CompletedTask;
        }
    }
}
