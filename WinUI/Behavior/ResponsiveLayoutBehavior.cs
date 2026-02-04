// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.WinUI.Behavior;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

/// <summary>
/// Behavior that monitors element size and updates a LayoutMode property
/// based on configurable breakpoints.
/// </summary>
public sealed class ResponsiveLayoutBehavior : Behavior<FrameworkElement>
{
    /// <summary>
    /// Breakpoint width for compact mode (below this width).
    /// </summary>
    public static readonly DependencyProperty CompactBreakpointProperty =
        DependencyProperty.Register(
            nameof(CompactBreakpoint),
            typeof(double),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata(640.0));

    /// <summary>
    /// Breakpoint width for wide mode (above this width).
    /// </summary>
    public static readonly DependencyProperty WideBreakpointProperty =
        DependencyProperty.Register(
            nameof(WideBreakpoint),
            typeof(double),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata(1024.0));

    /// <summary>
    /// The current layout mode (read-only, bindable).
    /// </summary>
    public static readonly DependencyProperty CurrentModeProperty =
        DependencyProperty.Register(
            nameof(CurrentMode),
            typeof(ResponsiveMode),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata(ResponsiveMode.Wide));

    /// <summary>
    /// Visual state name to apply for Compact mode.
    /// </summary>
    public static readonly DependencyProperty CompactStateNameProperty =
        DependencyProperty.Register(
            nameof(CompactStateName),
            typeof(string),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata("CompactState"));

    /// <summary>
    /// Visual state name to apply for Medium mode.
    /// </summary>
    public static readonly DependencyProperty MediumStateNameProperty =
        DependencyProperty.Register(
            nameof(MediumStateName),
            typeof(string),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata("MediumState"));

    /// <summary>
    /// Visual state name to apply for Wide mode.
    /// </summary>
    public static readonly DependencyProperty WideStateNameProperty =
        DependencyProperty.Register(
            nameof(WideStateName),
            typeof(string),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata("WideState"));

    public double CompactBreakpoint
    {
        get => (double)GetValue(CompactBreakpointProperty);
        set => SetValue(CompactBreakpointProperty, value);
    }

    public double WideBreakpoint
    {
        get => (double)GetValue(WideBreakpointProperty);
        set => SetValue(WideBreakpointProperty, value);
    }

    public ResponsiveMode CurrentMode
    {
        get => (ResponsiveMode)GetValue(CurrentModeProperty);
        private set => SetValue(CurrentModeProperty, value);
    }

    public string CompactStateName
    {
        get => (string)GetValue(CompactStateNameProperty);
        set => SetValue(CompactStateNameProperty, value);
    }

    public string MediumStateName
    {
        get => (string)GetValue(MediumStateNameProperty);
        set => SetValue(MediumStateNameProperty, value);
    }

    public string WideStateName
    {
        get => (string)GetValue(WideStateNameProperty);
        set => SetValue(WideStateNameProperty, value);
    }

    /// <summary>
    /// Raised when the layout mode changes.
    /// </summary>
    public event EventHandler<ResponsiveModeChangedEventArgs>? ModeChanged;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SizeChanged += OnSizeChanged;
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SizeChanged -= OnSizeChanged;
        AssociatedObject.Loaded -= OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateMode(AssociatedObject.ActualWidth);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateMode(e.NewSize.Width);
    }

    private void UpdateMode(double width)
    {
        var newMode = width switch
        {
            _ when width < CompactBreakpoint => ResponsiveMode.Compact,
            _ when width < WideBreakpoint => ResponsiveMode.Medium,
            _ => ResponsiveMode.Wide
        };

        if (newMode != CurrentMode)
        {
            var oldMode = CurrentMode;
            CurrentMode = newMode;

            // Apply visual state
            var stateName = newMode switch
            {
                ResponsiveMode.Compact => CompactStateName,
                ResponsiveMode.Medium => MediumStateName,
                ResponsiveMode.Wide or _ => WideStateName
            };

            VisualStateManager.GoToState(AssociatedObject as Control, stateName, true);

            ModeChanged?.Invoke(this, new ResponsiveModeChangedEventArgs(oldMode, newMode));
        }
    }
}

/// <summary>
/// Responsive layout modes.
/// </summary>
public enum ResponsiveMode
{
    /// <summary>Compact mode (e.g., phone, narrow window).</summary>
    Compact,

    /// <summary>Medium mode (e.g., tablet, medium window).</summary>
    Medium,

    /// <summary>Wide mode (e.g., desktop, wide window).</summary>
    Wide
}

/// <summary>
/// Event arguments for responsive mode changes.
/// </summary>
public sealed class ResponsiveModeChangedEventArgs : EventArgs
{
    public ResponsiveModeChangedEventArgs(ResponsiveMode oldMode, ResponsiveMode newMode)
    {
        OldMode = oldMode;
        NewMode = newMode;
    }

    public ResponsiveMode OldMode { get; }
    public ResponsiveMode NewMode { get; }
}
