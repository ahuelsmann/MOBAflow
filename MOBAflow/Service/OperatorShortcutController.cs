// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Moba.SharedUI.Service;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

/// <summary>
/// Window-wide operator shortcuts: Pause and Space stop the locomotive selected in train control, Ctrl+P and F10
/// toggle track power. <see cref="OperatorShortcuts"/> decides; this class only adapts WinUI keys and focus.
/// </summary>
internal static class OperatorShortcutController
{
    /// <summary>Listens to the window content before the focused element handles the key.</summary>
    /// <param name="window">The main window.</param>
    /// <param name="emergencyStop">The train-control emergency stop command.</param>
    /// <param name="setTrackPower">The track power command; its parameter is the requested state.</param>
    /// <param name="isTrackPowerOn">Reads the current track power state.</param>
    public static void Attach(Window window, ICommand emergencyStop, ICommand setTrackPower, Func<bool> isTrackPowerOn)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(emergencyStop);
        ArgumentNullException.ThrowIfNull(setTrackPower);
        ArgumentNullException.ThrowIfNull(isTrackPowerOn);
        if (window.Content is not UIElement root)
        {
            return;
        }

        root.PreviewKeyDown += (_, args) =>
        {
            var action = OperatorShortcuts.Resolve(
                ToShortcutKey(args.Key),
                IsDown(VirtualKey.Control),
                IsDown(VirtualKey.Menu),
                IsDown(VirtualKey.Shift),
                UsesSpace(FocusManager.GetFocusedElement(root.XamlRoot)));
            args.Handled = action switch
            {
                OperatorShortcutAction.EmergencyStop => TryExecute(emergencyStop, null),
                OperatorShortcutAction.ToggleTrackPower => TryExecute(setTrackPower, !isTrackPowerOn()),
                _ => false
            };
        };
    }

    private static bool TryExecute(ICommand command, object? parameter)
    {
        if (!command.CanExecute(parameter))
        {
            return false;
        }

        command.Execute(parameter);
        return true;
    }

    private static OperatorShortcutKey ToShortcutKey(VirtualKey key) => key switch
    {
        VirtualKey.Pause => OperatorShortcutKey.Pause,
        VirtualKey.Space => OperatorShortcutKey.Space,
        VirtualKey.P => OperatorShortcutKey.P,
        VirtualKey.F10 => OperatorShortcutKey.F10,
        _ => OperatorShortcutKey.Other
    };

    private static bool IsDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

    // Elements that act on Space themselves: buttons and toggles, text input, lists and menus.
    private static bool UsesSpace(object? focused) => focused is ButtonBase or ToggleSwitch or TextBox or PasswordBox
        or RichEditBox or AutoSuggestBox or NumberBox or ComboBox or ListViewBase or SelectorItem or TreeView
        or TreeViewItem or MenuFlyoutItemBase or NavigationViewItem or RatingControl;
}
