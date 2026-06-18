// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Domain;

using Moba.Common.Runtime;
using Moba.SharedUI.Service;

[TestFixture]
internal sealed class SignalBoxRuntimeSyncTests
{
    [Test]
    public void ApplyToPlan_Should_UpdateSignalAspectOnEditorModel()
    {
        var signalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var plan = new SignalBoxPlan
        {
            Elements =
            [
                new SbSignal
                {
                    Id = signalId,
                    Name = "G2HBFA1",
                    SignalAspect = SignalAspect.Dunkel
                }
            ]
        };

        var changed = SignalBoxRuntimeSync.ApplyToPlan(
            plan,
            [
                new SignalBoxElementRuntimeSnapshot
                {
                    ElementId = signalId,
                    Kind = SignalBoxElementKind.Signal,
                    SignalAspect = SignalAspect.Hp0
                }
            ]);

        Assert.That(changed, Is.True);
        Assert.That(plan.Elements.OfType<SbSignal>().Single().SignalAspect, Is.EqualTo(SignalAspect.Hp0));
    }
}
