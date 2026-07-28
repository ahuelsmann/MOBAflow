// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Reusable WinUI input adapter for selected-object presentation and commands.
/// </summary>
public sealed partial class SelectedObjectWorkbench : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(InterlockingControlViewModel),
        typeof(SelectedObjectWorkbench),
        new PropertyMetadata(null));

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

    public InterlockingControlViewModel ViewModel
    {
        get => (InterlockingControlViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

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
}
