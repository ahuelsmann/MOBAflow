// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Behavior;

using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using SharedUI.ViewModel;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Behavior that enables drop functionality for Workflow assignment.
/// Attach to any UIElement (e.g., Border, Grid) to accept Workflow drops.
/// </summary>
public sealed class WorkflowDropBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(WorkflowDropBehavior),
            new PropertyMetadata(null));

    /// <summary>
    /// Command to execute when a Workflow is dropped.
    /// The WorkflowViewModel is passed as the command parameter.
    /// </summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.AllowDrop = true;
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.Drop += OnDrop;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.DragOver -= OnDragOver;
            AssociatedObject.Drop -= OnDrop;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        // Accept only Workflow drops
        if (e.DataView.Properties.ContainsKey("Workflow"))
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "Assign Workflow";
            e.DragUIOverride.IsCaptionVisible = true;
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("Workflow", out object? workflowObj) &&
            workflowObj is WorkflowViewModel workflow)
        {
            Command?.Execute(workflow);
        }
    }
}
