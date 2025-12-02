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
        
        var journey = new Journey 
        { 
            Name = "J1", 
            InPort = 1, 
            Stations = new List<Station> 
            { 
                new Station { Name = "S1", NumberOfLapsToStop = 1 } 
            } 
        };
        var journeys = new List<Journey> { journey };

        var executionContext = new ActionExecutionContext
        {
            Z21 = z21Mock.Object
        };

        using var manager = new JourneyManager(z21Mock.Object, journeys, workflowService, executionContext);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Monitor journey property changes via polling (since StateChanged event removed)
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var monitorTask = Task.Run(async () =>
        {
            while (!cancellationToken.Token.IsCancellationRequested)
            {
                if (journey.CurrentCounter == 0 && journey.CurrentPos == 0)
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
        Assert.That(journey.CurrentCounter, Is.EqualTo(0), "Counter should be reset after reaching target");
        Assert.That(journey.CurrentPos, Is.EqualTo(0), "Position should be reset");
    }
}
