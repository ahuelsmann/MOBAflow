// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Service;

using Common.Runtime;

using Domain;

/// <summary>
/// Applies runtime signal-box snapshots onto the editable project model shown in MOBAflow.
/// </summary>
public static class SignalBoxRuntimeSync
{
    public static bool ApplyToPlan(SignalBoxPlan? plan, IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements)
    {
        if (plan == null || elements.Count == 0)
        {
            return false;
        }

        var changed = false;

        foreach (var snapshot in elements)
        {
            if (plan.FindElement(snapshot.ElementId) is not SbElement element)
            {
                continue;
            }

            if (element is SbSignal signal
                && snapshot.SignalAspect is { } aspect
                && signal.SignalAspect != aspect)
            {
                signal.SignalAspect = aspect;
                changed = true;
            }

            if (element is SbSwitch sw
                && snapshot.SwitchPosition is { } position
                && sw.SwitchPosition != position)
            {
                sw.SwitchPosition = position;
                changed = true;
            }
        }

        return changed;
    }
}
