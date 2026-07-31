// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Moves keyboard focus to a control after a button invokes its command.
/// </summary>
internal static class FocusTargetBehavior
{
    public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
        "Target",
        typeof(Control),
        typeof(FocusTargetBehavior),
        new PropertyMetadata(null, OnTargetChanged));

    public static Control? GetTarget(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (Control?)element.GetValue(TargetProperty);
    }

    public static void SetTarget(DependencyObject element, Control? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TargetProperty, value);
    }

    private static void OnTargetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not ButtonBase button)
            return;

        button.Click -= OnButtonClick;
        if (args.NewValue is Control)
            button.Click += OnButtonClick;
    }

    private static void OnButtonClick(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (sender is not ButtonBase button || GetTarget(button) is not Control target)
            return;

        target.DispatcherQueue.TryEnqueue(() =>
        {
            if (target is SelectedObjectWorkbench workbench)
                workbench.FocusWorkbench();
            else
                target.Focus(FocusState.Programmatic);
        });
    }
}
