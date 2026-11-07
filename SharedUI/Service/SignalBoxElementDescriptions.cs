// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Domain;

/// <summary>
/// Spoken description of a signal-box element for screen readers, for example "Signal A1, Ks1" or
/// "Switch W2, diverging left, occupied".
/// </summary>
public static class SignalBoxElementDescriptions
{
    /// <summary>Describes the kind, name and current state of an element.</summary>
    public static string Describe(SbElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var parts = new List<string> { Title(element) };
        switch (element)
        {
            case SbSignal signal:
                parts.Add(signal.SignalAspect.ToString());
                break;
            case SbSwitch @switch:
                parts.Add(@switch.SwitchPosition switch
                {
                    SwitchPosition.DivergingLeft => "diverging left",
                    SwitchPosition.DivergingRight => "diverging right",
                    _ => "straight"
                });
                break;
        }

        if (element.State != SignalBoxElementState.Free)
        {
            parts.Add(element.State == SignalBoxElementState.Occupied ? "occupied" : "fault");
        }

        return string.Join(", ", parts);
    }

    private static string Title(SbElement element)
    {
        var kind = element switch
        {
            SbSignal => "Signal",
            SbSwitch => "Switch",
            SbDetector => "Feedback detector",
            SbTrackCurve => "Curved track",
            _ => "Track"
        };
        return string.IsNullOrWhiteSpace(element.Name) ? kind : $"{kind} {element.Name.Trim()}";
    }
}
