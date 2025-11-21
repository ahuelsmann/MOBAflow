// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Moba.Test.Mocks;

namespace Moba.Test.Backend;

[TestFixture]
public class Z21UnitTests
{
    [Test]
    public void SimulateFeedback_RaisesReceivedEvent()
    {
        var fakeUdp = new FakeUdpClientWrapper();
        using var z21 = new Z21(fakeUdp, null);

        FeedbackResult? captured = null;
        var signaled = new TaskCompletionSource<bool>();

        z21.Received += (f) => {
            captured = f;
            signaled.TrySetResult(true);
        };

        z21.SimulateFeedback(7);

        // wait briefly
        _ = Task.WhenAny(signaled.Task, Task.Delay(500)).Result;
        Assert.That(signaled.Task.IsCompleted, Is.True, "Received event was not raised");
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.InPort, Is.EqualTo(7u));
    }
}
