// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

/// <summary>The button the operator chose in a dialog with three choices.</summary>
public enum DialogChoice
{
    /// <summary>The primary button, for example "Save".</summary>
    Primary,

    /// <summary>The secondary button, for example "Don't save".</summary>
    Secondary,

    /// <summary>The cancel button, or the dialog was dismissed.</summary>
    Cancel,

    /// <summary>No dialog could be shown, for example because the host has no window; the caller decides.</summary>
    NotShown
}
