// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Tile in a group where exactly one tile is current, such as the signal aspects. Activating a tile only checks it,
/// as a radio button does, so confirming the current tile never switches it off first and Narrator does not announce
/// "off". <see cref="ButtonBase.Click"/> is still raised; the owner unchecks the previous tile there.
/// </summary>
internal sealed partial class SelectionToggleButton : ToggleButton
{
    protected override void OnToggle()
    {
        IsChecked = true;
    }
}
