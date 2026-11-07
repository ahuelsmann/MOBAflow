// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

/// <summary>Keys that can trigger an operator shortcut; every other key maps to <see cref="Other"/>.</summary>
public enum OperatorShortcutKey
{
    /// <summary>Any key without an operator shortcut.</summary>
    Other,

    /// <summary>The Pause key.</summary>
    Pause,

    /// <summary>The Space bar.</summary>
    Space,

    /// <summary>The P key.</summary>
    P,

    /// <summary>The F10 key.</summary>
    F10
}

/// <summary>Operator action triggered by a shortcut.</summary>
public enum OperatorShortcutAction
{
    /// <summary>The key is not an operator shortcut and keeps its normal meaning.</summary>
    None,

    /// <summary>Stop the locomotive selected in train control.</summary>
    EmergencyStop,

    /// <summary>Switch track power on when it is off, and off when it is on.</summary>
    ToggleTrackPower
}

/// <summary>
/// Maps keys to operator safety actions. Pause always stops; Space stops only while the focused element does not use
/// Space itself, so focused buttons, toggles, lists and text inputs keep working. Ctrl+P and F10 toggle track power.
/// </summary>
public static class OperatorShortcuts
{
    /// <summary>Resolves the action for a pressed key.</summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="control">Whether Ctrl is held.</param>
    /// <param name="alt">Whether Alt is held.</param>
    /// <param name="shift">Whether Shift is held.</param>
    /// <param name="focusUsesSpace">Whether the focused element handles Space itself.</param>
    public static OperatorShortcutAction Resolve(OperatorShortcutKey key, bool control, bool alt, bool shift, bool focusUsesSpace)
    {
        var noModifier = !control && !alt && !shift;
        return key switch
        {
            OperatorShortcutKey.Pause => OperatorShortcutAction.EmergencyStop,
            OperatorShortcutKey.Space when noModifier && !focusUsesSpace => OperatorShortcutAction.EmergencyStop,
            OperatorShortcutKey.P when control && !alt && !shift => OperatorShortcutAction.ToggleTrackPower,
            OperatorShortcutKey.F10 when noModifier => OperatorShortcutAction.ToggleTrackPower,
            _ => OperatorShortcutAction.None
        };
    }
}
