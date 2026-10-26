// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using global::Moba.Backend.Interface;
using global::Moba.Backend.Service;
using global::Moba.Common.Events;
using global::Moba.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Common.Configuration;
using Moba.Test.Helpers;
using Moba.Test.Mocks;
using Moba.Test.TestData;
using System.Threading.Channels;

internal sealed class LocomotiveWhistleAutomationServiceTests
{
    private static readonly string[] AllRuleErrors =
    [
        "A locomotive is required.",
        "Feedback input must be positive.",
        "Function must be between F0 and F31.",
        "Delay must not be negative.",
        "Active duration must be positive."
    ];

    [Test]
    public async Task CounterFilteredActivation_StillReachesRawWhistleConsumer()
    {
        // Both consumers are fed by the real receive path: R-Bus packets -> Z21 -> counter and shared bus.
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        using var udp = new FakeUdpClientWrapper();
        var z21 = new Z21(udp, bus);
        try
        {
            using var counters = new InPortCounterService(z21, new AppSettings
            {
                Counter = { CountOfFeedbackPoints = 5, UseTimerFilter = true, TimerIntervalSeconds = 3600 }
            }, clock);
            var counted = 0;
            counters.Counted += (_, _) => Interlocked.Increment(ref counted);
            var gateway = new RecordingGateway();
            using var whistles = new LocomotiveWhistleAutomationService(bus, gateway,
                NullLogger<LocomotiveWhistleAutomationService>.Instance, clock);
            var locomotive = new Locomotive { DigitalAddress = 12 };
            whistles.Activate(new Project
            {
                Locomotives = [locomotive],
                LocomotiveWhistleRules = [new() { LocomotiveId = locomotive.Id, InPort = 5, FunctionIndex = 2, ActiveDurationMilliseconds = 1000 }]
            });

            // Subscribed after the whistle service, so a recorded event has already been handed to it.
            var rawFeedback = Channel.CreateUnbounded<int>();
            bus.Subscribe<FeedbackReceivedEvent>(feedback => rawFeedback.Writer.TryWrite(feedback.InPort));
            var released = Z21Packets.RBusFeedbackInPort5.ToArray();
            released[5] = 0;
            (int, int, bool) on = (12, 2, true);
            (int, int, bool) off = (12, 2, false);
            // The service starts its "off" timer on a thread-pool continuation after "on"; the clock may only
            // move once that timer exists, and "off" arrives on another continuation after the timer fired.
            var idleTimers = clock.PendingTimerCount;

            udp.RaiseReceived(Z21Packets.RBusFeedbackInPort5);
            var firstRaw = await NextAsync(rawFeedback.Reader).ConfigureAwait(false);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRaw, Is.EqualTo(5));
                Assert.That(gateway.Commands, Is.EqualTo(new[] { on }));
            }
            await WaitUntilAsync(() => clock.PendingTimerCount > idleTimers).ConfigureAwait(false);
            clock.Advance(TimeSpan.FromSeconds(1));
            await WaitUntilAsync(() => gateway.Commands.Count == 2).ConfigureAwait(false);
            Assert.That(gateway.Commands, Is.EqualTo(new[] { on, off }));

            udp.RaiseReceived(released);
            udp.RaiseReceived(Z21Packets.RBusFeedbackInPort5);
            var secondRaw = await NextAsync(rawFeedback.Reader).ConfigureAwait(false);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondRaw, Is.EqualTo(5));
                Assert.That(gateway.Commands, Is.EqualTo(new[] { on, off, on }));
            }
            await WaitUntilAsync(() => clock.PendingTimerCount > idleTimers).ConfigureAwait(false);
            clock.Advance(TimeSpan.FromSeconds(1));
            await WaitUntilAsync(() => gateway.Commands.Count == 4).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gateway.Commands, Is.EqualTo(new[] { on, off, on, off }));
                Assert.That(counters.GetSnapshot().Single(counter => counter.InPort == 5).Count, Is.EqualTo(1UL));
                Assert.That(counted, Is.EqualTo(1));
                Assert.That(rawFeedback.Reader.TryRead(out _), Is.False);
            }
        }
        finally
        {
            await z21.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static Task<int> NextAsync(ChannelReader<int> reader) =>
        reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail("The whistle service did not reach the expected state within 5 seconds.");
            await Task.Delay(10).ConfigureAwait(false);
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

        Assert.That(errors, Is.EqualTo(AllRuleErrors));
    }

    [Test]
    public void Validate_AcceptsTheBoundaryValues()
    {
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        using var service = new LocomotiveWhistleAutomationService(
            eventBus, new RecordingGateway(), NullLogger<LocomotiveWhistleAutomationService>.Instance);
        var locomotiveId = Guid.NewGuid();

        var lowest = service.Validate(new LocomotiveWhistleRule
        {
            LocomotiveId = locomotiveId, InPort = 1, FunctionIndex = 0, DelayMilliseconds = 0, ActiveDurationMilliseconds = 1
        });
        var highest = service.Validate(new LocomotiveWhistleRule { LocomotiveId = locomotiveId, InPort = 1, FunctionIndex = 31 });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lowest, Is.Empty);
            Assert.That(highest, Is.Empty);
        }
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
