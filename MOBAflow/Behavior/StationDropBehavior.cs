// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Behavior;

using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using Moba.SharedUI.ViewModel;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

/// <summary>Forwards station-assignment drops to a ViewModel command.</summary>
public sealed class StationDropBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(StationDropBehavior), new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject == null) return;
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragOver += OnDragOver;
        AssociatedObject.Drop += OnDrop;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.DragOver -= OnDragOver;
            AssociatedObject.Drop -= OnDrop;
        }
        base.OnDetaching();
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Properties.ContainsKey("StationAssignment")) return;

        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = "Assign stop";
        e.DragUIOverride.IsCaptionVisible = true;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("StationAssignment", out var value)
            && value is StationAssignmentOption option)
        {
            Command?.Execute(option);
            e.Handled = true;
        }
    }
}
