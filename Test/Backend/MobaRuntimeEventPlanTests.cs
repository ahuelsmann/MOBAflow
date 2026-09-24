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
    public async Task CountersSurviveProjectActivationAndResetOnlyExplicitly(bool useCustomFactory)
    {
        var z21 = new Mock<IZ21>();
        using var runtime = CreateRuntime(z21.Object, useCustomFactory);
        Feedback(z21, 1);
        Feedback(z21, 1);
        var journey = new Journey { IsActive = true };
        await runtime.ActivateProjectAsync(new Project { Journeys = [journey] });
        await runtime.ActivateProjectAsync(new Project());

        Assert.That(runtime.Current.InPortCounters.Single(counter => counter.InPort == 1).Count, Is.EqualTo(2));

        await runtime.ResetInPortCountersAsync();

        Assert.That(runtime.Current.InPortCounters.All(counter => counter.Count == 0), Is.True);
    }

    [Test]
    public async Task ActivatedProject_ProjectsThePersistedActiveFlagAndRunsActiveJourneys()
    {
        var z21 = new Mock<IZ21>();
        var workflow = new Workflow { Name = "Announcement" };
        var executed = new List<Guid>();
        var workflows = new Mock<IWorkflowService>();
        workflows.Setup(service => service.ExecuteAsync(It.IsAny<WorkflowExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowExecutionRequest request, CancellationToken _) =>
            {
                lock (executed) executed.Add(request.Context.CurrentJourney!.Id);
                return new WorkflowExecutionResult
                {
                    ExecutionId = Guid.NewGuid(),
                    WorkflowId = request.Workflow.Id,
                    SourceCorrelationId = request.SourceCorrelationId,
                    Status = WorkflowExecutionStatus.Succeeded
                };
            });
        using var runtime = CreateRuntime(z21.Object, workflowService: workflows.Object);
        var active = new Journey { IsActive = true, EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { WorkflowId = workflow.Id }] } };
        var inactive = new Journey { EventPlan = new JourneyEventPlan { Events = [new JourneyEvent { WorkflowId = workflow.Id }] } };
        await runtime.ActivateProjectAsync(new Project { Journeys = [active, inactive], Workflows = [workflow] });

        Feedback(z21, 1);

        await WaitUntilAsync(() => { lock (executed) return executed.Count == 1; }).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(runtime.Current.JourneyStates[active.Id].IsActive, Is.True);
            Assert.That(runtime.Current.JourneyStates[inactive.Id].IsActive, Is.False);
            Assert.That(executed, Is.EqualTo(new[] { active.Id }));
        });
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            Assert.That(DateTime.UtcNow, Is.LessThan(timeout), "The condition was not met in time.");
            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21, bool useCustomFactory = false, IWorkflowService? workflowService = null)
    {
        var workflows = workflowService ?? Mock.Of<IWorkflowService>();
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
