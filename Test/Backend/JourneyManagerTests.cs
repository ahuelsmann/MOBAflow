// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moq;
using Moba.Backend.Manager;

namespace Moba.Test.Backend;

public class JourneyManagerTests
{
    [Test]
    public async Task ProcessFeedback_IncrementsJourneyCounter()
    {
        // Arrange
        var z21Mock = new Mock<IZ21>();
        var actionExecutorMock = new Mock<ActionExecutor>(z21Mock.Object);
        var workflowService = new WorkflowService(actionExecutorMock.Object, z21Mock.Object);
        
        var station = new Station { Id = Guid.NewGuid(), Name = "S1", NumberOfLapsToStop = 1 };
        var journey = new Journey 
        { 
            Id = Guid.NewGuid(),
            Name = "J1", 
            InPort = 1,
            FirstPos = 0,
            StationIds = new List<Guid> { station.Id }
        };
        
        var project = new Project
        {
            Stations = new List<Station> { station },
            Journeys = new List<Journey> { journey }
        };

        var executionContext = new ActionExecutionContext
        {
            Z21 = z21Mock.Object
        };

        using var manager = new JourneyManager(z21Mock.Object, project, workflowService, executionContext);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Monitor SessionState via GetState instead of journey properties
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var monitorTask = Task.Run(async () =>
        {
            while (!cancellationToken.Token.IsCancellationRequested)
            {
                var state = manager.GetState(journey.Id);
                if (state != null && state.Counter == 0 && state.CurrentPos == 0)
                {
                    tcs.TrySetResult(true);
                    return;
                }
                await Task.Delay(50, cancellationToken.Token);
            }
        }, cancellationToken.Token);

        // Act - Simulate feedback by calling Z21's Received event
        z21Mock.Raise(z => z.Received += null, new FeedbackResult([0x0F, 0x00, 0x80, 0x00, 0x00, 0x01]));

        // Wait for reset with timeout
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));

        cancellationToken.Cancel();

        // Assert
        Assert.That(completed == tcs.Task, Is.True, "Processing did not complete in time");
        var finalState = manager.GetState(journey.Id);
        Assert.That(finalState, Is.Not.Null, "SessionState should exist");
        Assert.That(finalState!.Counter, Is.EqualTo(0), "Counter should be reset after reaching target");
        Assert.That(finalState.CurrentPos, Is.EqualTo(0), "Position should be reset");
    }
}
