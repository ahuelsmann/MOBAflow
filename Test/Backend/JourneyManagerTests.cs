using Moq;
using Moba.Backend.Manager;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend;

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

        using var manager = new JourneyManager(z21Mock.Object, journeys);

        // Use TaskCompletionSource to wait for processing
        var tcs = new TaskCompletionSource<bool>();

        // Hook into Journey changes
        journey.StateChanged += (_, _) => tcs.TrySetResult(true);

        // Act: simulate feedback by invoking the Received event
        await z21Mock.RaiseAsync(z => z.Received += null, new FeedbackResult(new byte[] { 0x0F, 0x00, 0x80, 0x00, 0x00, 0x01 }));

        // Wait for processing (timeout-safe)
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.That(completed == tcs.Task, Is.True, "Processing did not complete in time");
        Assert.That(journey.CurrentCounter, Is.EqualTo(0)); // Should have reset after station
        Assert.That(journey.CurrentPos, Is.EqualTo(0));
    }
}
