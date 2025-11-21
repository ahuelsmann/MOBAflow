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
        var journey = new Journey { Name = "J1", InPort = 1, Stations = new List<Station> { new Station { Name = "S1", NumberOfLapsToStop = 1 } } };
        var journeys = new List<Journey> { journey };

        var executionContext = new ActionExecutionContext
        {
            Z21 = z21Mock.Object
        };

        using var manager = new JourneyManager(z21Mock.Object, journeys, executionContext);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Resolve only when reset occurred (after reaching station)
        journey.StateChanged += (_, _) =>
        {
            if (journey.CurrentCounter == 0)
            {
                tcs.TrySetResult(true);
            }
        };

        // Act - Simulate feedback by calling Z21's Received event
        z21Mock.Raise(z => z.Received += null, new FeedbackResult([0x0F, 0x00, 0x80, 0x00, 0x00, 0x01]));

        // Wait for reset with timeout
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));

        // Assert
        Assert.That(completed == tcs.Task, Is.True, "Processing did not complete in time");
        Assert.That(journey.CurrentCounter, Is.EqualTo(0), "Counter should be reset after reaching target");
        Assert.That(journey.CurrentPos, Is.EqualTo(0), "Position should be reset");
    }
}
