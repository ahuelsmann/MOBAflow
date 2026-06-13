// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

[TestFixture]
internal sealed class OperatingStateEvaluatorTests
{
    [Test]
    public void Evaluate_ReturnsFailSafe_WhenConnectionLostAfterSuccessfulConnect()
    {
        var presentation = OperatingStateEvaluator.Evaluate(new OperatingStateInput
        {
            IsConnected = false,
            HasSeenSuccessfulZ21Connection = true,
            IsZ21Connecting = false,
            LastFailSafeReason = "Unexpected loss of the Z21 connection."
        });

        Assert.That(presentation.State, Is.EqualTo(OperatingStateKind.FailSafe));
        Assert.That(presentation.IsFailSafeActive, Is.True);
        Assert.That(presentation.ShowInfoBar, Is.True);
    }

    [Test]
    public void Evaluate_ReturnsDegraded_WhenRestApiUnreachable()
    {
        var presentation = OperatingStateEvaluator.Evaluate(new OperatingStateInput
        {
            IsConnected = true,
            AutoStartWebApp = true,
            RestApiIsReachable = false
        });

        Assert.That(presentation.State, Is.EqualTo(OperatingStateKind.Degraded));
        Assert.That(presentation.DetailText, Does.Contain("REST API not reachable"));
    }
}
