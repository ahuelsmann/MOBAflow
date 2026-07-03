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

    /// <summary>
    /// Keeps only cached runtime elements that still exist in the active signal-box plan.
    /// </summary>
    public static IReadOnlyList<SignalBoxElementRuntimeSnapshot> FilterToPlan(
        SignalBoxPlan? plan,
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements)
    {
        if (plan == null || elements.Count == 0)
        {
            return elements;
        }

        return elements
            .Where(element => plan.FindElement(element.ElementId) != null)
            .ToList();
    }

    /// <summary>
    /// Builds runtime snapshots from the editable signal-box plan (membership and layout metadata).
    /// </summary>
    public static IReadOnlyList<SignalBoxElementRuntimeSnapshot> BuildSnapshotsFromPlan(SignalBoxPlan? plan)
    {
        if (plan == null || plan.Elements.Count == 0)
        {
            return [];
        }

        var snapshots = new List<SignalBoxElementRuntimeSnapshot>(plan.Elements.Count);

        foreach (var element in plan.Elements)
        {
            switch (element)
            {
                case SbSignal signal:
                    snapshots.Add(new SignalBoxElementRuntimeSnapshot
                    {
                        ElementId = signal.Id,
                        Name = signal.Name,
                        Kind = SignalBoxElementKind.Signal,
                        X = signal.X,
                        Y = signal.Y,
                        SignalSystem = signal.SignalSystem,
                        SignalAspect = signal.SignalAspect,
                        MainSignalArticleNumber = signal.MainSignalArticleNumber,
                        MultiplexerArticleNumber = signal.MultiplexerArticleNumber,
                        TopSpeedIndicator = signal.TopSpeedIndicator,
                        BottomSpeedIndicator = signal.BottomSpeedIndicator
                    });
                    break;

                case SbSwitch sw:
                    snapshots.Add(new SignalBoxElementRuntimeSnapshot
                    {
                        ElementId = sw.Id,
                        Name = sw.Name,
                        Kind = SignalBoxElementKind.Switch,
                        X = sw.X,
                        Y = sw.Y,
                        Address = sw.Address,
                        SwitchPosition = sw.SwitchPosition
                    });
                    break;
            }
        }

        return snapshots;
    }
}
