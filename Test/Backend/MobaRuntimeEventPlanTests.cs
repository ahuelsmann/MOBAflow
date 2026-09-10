// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moq;

[TestFixture]
internal sealed class MobaRuntimeEventPlanTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task CountersSurviveProjectActivationAndUseIndependentStartValues(bool useCustomFactory)
    {
        var z21 = new Mock<IZ21>();
        using var runtime = CreateRuntime(z21.Object, useCustomFactory);
        Feedback(z21, 1);
        Feedback(z21, 1);
        Feedback(z21, 2);
        var journey = new Journey { EventPlan = new JourneyEventPlan(),
            FeedbackSequence = [new JourneyFeedbackStep { InPort = 8, Index = 4 }] };
        var project = new Project { Journeys = [journey] };
        await runtime.ActivateProjectAsync(project);
        Assert.That(runtime.Current.JourneyStates[journey.Id].IsActive, Is.False);

        await runtime.StartJourneyAsync(journey.Id);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.Current.JourneyStates[journey.Id].StartCounterValues[1], Is.EqualTo(2));
            Assert.That(runtime.Current.JourneyStates[journey.Id].StartCounterValues[2], Is.EqualTo(1));
            Assert.That(runtime.Current.CanResetInPortCounters, Is.False);
            Assert.That(runtime.Current.JourneyStates[journey.Id].ExpectedInPort, Is.Null,
                "Retained legacy steps do not describe the active event plan.");
        });
        Assert.ThrowsAsync<InvalidOperationException>(() => runtime.ResetInPortCountersAsync());
        Assert.ThrowsAsync<InvalidOperationException>(() => runtime.ResetJourneyAsync(journey.Id));
        await runtime.StopJourneyAsync(journey.Id);
        await runtime.ActivateProjectAsync(new Project());
        Assert.That(runtime.Current.InPortCounters.Single(counter => counter.InPort == 1).Count, Is.EqualTo(2));
        await runtime.ResetInPortCountersAsync();
        Assert.That(runtime.Current.InPortCounters.All(counter => counter.Count == 0), Is.True);
    }

    [Test]
    public async Task EditorRefreshPreservesRunningJourneyAndSecondJourneyCapturesLaterCounts()
    {
        var z21 = new Mock<IZ21>();
        using var runtime = CreateRuntime(z21.Object);
        var first = new Journey { EventPlan = new JourneyEventPlan() };
        var second = new Journey { EventPlan = new JourneyEventPlan() };
        var project = new Project { Journeys = [first, second] };
        await runtime.ActivateProjectAsync(project);
        await runtime.StartJourneyAsync(first.Id);
        var runId = runtime.Current.JourneyStates[first.Id].JourneyRunId;
        Feedback(z21, 1);
        await runtime.ActivateProjectAsync(project);
        await runtime.StartJourneyAsync(second.Id);

        Assert.Multiple(() =>
        {
            Assert.That(runtime.Current.JourneyStates[first.Id].JourneyRunId, Is.EqualTo(runId));
            Assert.That(runtime.Current.JourneyStates[first.Id].StartCounterValues[1], Is.Zero);
            Assert.That(runtime.Current.JourneyStates[second.Id].StartCounterValues[1], Is.EqualTo(1));
        });
        await runtime.StopJourneyAsync(first.Id);
        Assert.ThrowsAsync<InvalidOperationException>(() => runtime.ResetInPortCountersAsync());
        await runtime.StopJourneyAsync(second.Id);
        Assert.That(runtime.Current.CanResetInPortCounters, Is.True);
    }

    [Test]
    public async Task StartingChangedJourneyWhileAnotherRunsRejectsStaleDefinition()
    {
        var z21 = new Mock<IZ21>();
        using var runtime = CreateRuntime(z21.Object);
        var first = new Journey { EventPlan = new JourneyEventPlan() };
        var second = new Journey { EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { Count = 2 }] } };
        var project = new Project { Journeys = [first, second] };
        await runtime.ActivateProjectAsync(project);
        await runtime.StartJourneyAsync(first.Id);
        var firstRun = runtime.Current.JourneyStates[first.Id].JourneyRunId;
        second.EventPlan.Events[0].Count = 5;
        await runtime.ActivateProjectAsync(project);

        Assert.ThrowsAsync<InvalidOperationException>(() => runtime.StartJourneyAsync(second.Id));
        Assert.That(runtime.Current.JourneyStates[first.Id].JourneyRunId, Is.EqualTo(firstRun));
        Assert.That(runtime.Current.JourneyStates[second.Id].IsActive, Is.False);
        await runtime.StopJourneyAsync(first.Id);
        await runtime.StartJourneyAsync(second.Id);
        Assert.That(runtime.Current.JourneyStates[second.Id].IsActive, Is.True);
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21, bool useCustomFactory = false)
    {
        var workflows = Mock.Of<IWorkflowService>();
        return new MobaRuntimeService(z21, workflows,
            new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = z21 }),
            new AppSettings { Counter = new CounterSettings { CountOfFeedbackPoints = 3, UseTimerFilter = false } },
            NullLogger<MobaRuntimeService>.Instance,
            journeyManagerFactory: useCustomFactory ? new Moba.Backend.Manager.JourneyManagerFactory(z21, workflows) : null);
    }

    private static void Feedback(Mock<IZ21> z21, int inPort)
    {
        byte[] packet = [0x0F, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        packet[5 + (inPort - 1) / 8] = (byte)(1 << ((inPort - 1) % 8));
        z21.Raise(source => source.Received += null, new FeedbackResult(packet));
    }
}
