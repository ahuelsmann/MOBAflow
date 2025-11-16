using System.Net;
using Moba.Test.Z21Simulator;
using Moba.Backend;

namespace Moba.Test.Backend;

[TestFixture]
public class Z21IntegrationTests
{
    [Test]
    public async Task Z21_Receives_SimulatedFeedback_FromSimulator()
    {
        using var z21 = new Z21(null, null);

        var received = new TaskCompletionSource<FeedbackResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        z21.Received += (f) => received.TrySetResult(f);

        // Use internal simulation helper for deterministic test
        z21.SimulateFeedback(5);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var task = await Task.WhenAny(received.Task, Task.Delay(Timeout.Infinite, cts.Token));
        if (task != received.Task)
            Assert.Fail("Timeout waiting for Z21 Received event");

        var result = received.Task.Result;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.InPort, Is.EqualTo(5u));
    }
}
