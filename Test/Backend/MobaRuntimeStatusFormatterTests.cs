// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend;
using Moba.Backend.Service;

[TestFixture]
internal sealed class MobaRuntimeStatusFormatterTests
{
    [Test]
    public void BuildSystemStateStatusText_IncludesEmergencyStop()
    {
        var text = MobaRuntimeStatusFormatter.BuildSystemStateStatusText(new SystemState
        {
            CentralState = 0x01
        });

        Assert.That(text, Does.Contain("EMERGENCY STOP"));
    }

    [Test]
    public void GetDisconnectedStatusText_UsesManualDisconnectLabel()
    {
        Assert.That(
            MobaRuntimeStatusFormatter.GetDisconnectedStatusText(isManualDisconnectRequested: true),
            Is.EqualTo("Disconnected"));
    }
}
