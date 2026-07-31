// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Reusable WinUI input adapter for selected-object presentation and commands.
/// </summary>
internal sealed partial class SelectedObjectWorkbench : UserControl
{
    public static readonly DependencyProperty ContextProperty = DependencyProperty.Register(
        nameof(Context),
        typeof(object),
        typeof(SelectedObjectWorkbench),
        new PropertyMetadata(null, OnContextChanged));

    public static readonly DependencyProperty DefinitionContentProperty = DependencyProperty.Register(
        nameof(DefinitionContent),
        typeof(UIElement),
        typeof(SelectedObjectWorkbench),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ShowRouteAuthoringProperty = DependencyProperty.Register(
        nameof(ShowRouteAuthoring),
        typeof(bool),
        typeof(SelectedObjectWorkbench),
        new PropertyMetadata(false));

    public SelectedObjectWorkbench()
    {
        InitializeComponent();
    }

    public object? Context
    {
        get => GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    internal InterlockingControlViewModel? ViewModel => Context as InterlockingControlViewModel;

    public UIElement? DefinitionContent
    {
        get => (UIElement?)GetValue(DefinitionContentProperty);
        set => SetValue(DefinitionContentProperty, value);
    }

    public bool ShowRouteAuthoring
    {
        get => (bool)GetValue(ShowRouteAuthoringProperty);
        set => SetValue(ShowRouteAuthoringProperty, value);
    }

    /// <summary>
    /// Moves keyboard focus into the workbench after an explicit Context invocation.
    /// </summary>
    public bool FocusWorkbench() =>
        this.WorkbenchScrollViewer.Focus(FocusState.Programmatic);

    private static void OnContextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is SelectedObjectWorkbench workbench)
        {
            workbench.Bindings.Update();
        }
    }
}
