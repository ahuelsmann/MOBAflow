// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.Matrix;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System.Windows.Input;

public sealed partial class View5x5 : UserControl
{
    private enum DragMode
    {
        None,
        Paint,
        Clear
    }

    public static readonly DependencyProperty CellTappedCommandProperty =
        DependencyProperty.Register(nameof(CellTappedCommand), typeof(ICommand), typeof(View5x5), new PropertyMetadata(null));

    public static readonly DependencyProperty CellRightTappedCommandProperty =
        DependencyProperty.Register(nameof(CellRightTappedCommand), typeof(ICommand), typeof(View5x5), new PropertyMetadata(null));

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(ViewModel5x5), typeof(View5x5), new PropertyMetadata(null, OnViewModelChanged));

    public ViewModel5x5 ViewModel
    {
        get => (ViewModel5x5)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ICommand? CellTappedCommand
    {
        get => (ICommand?)GetValue(CellTappedCommandProperty);
        set => SetValue(CellTappedCommandProperty, value);
    }

    public ICommand? CellRightTappedCommand
    {
        get => (ICommand?)GetValue(CellRightTappedCommandProperty);
        set => SetValue(CellRightTappedCommandProperty, value);
    }

    public View5x5()
    {
        InitializeComponent();
        ViewModel = new ViewModel5x5();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is View5x5 view)
        {
            view.DataContext = e.NewValue;
        }
    }

    private readonly HashSet<int> processedDragCells = [];
    private DragMode dragMode = DragMode.None;

    private void OnMatrixPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement matrixGrid)
        {
            return;
        }

        var point = e.GetCurrentPoint(matrixGrid);
        var properties = point.Properties;

        dragMode = properties.IsRightButtonPressed
            ? DragMode.Clear
            : properties.IsLeftButtonPressed
                ? DragMode.Paint
                : DragMode.None;

        processedDragCells.Clear();

        if (dragMode == DragMode.None)
        {
            return;
        }

        matrixGrid.CapturePointer(e.Pointer);
        ExecuteCellCommand(GetCellIndex(point.Position, matrixGrid));
        e.Handled = true;
    }

    private void OnMatrixPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (dragMode == DragMode.None)
        {
            return;
        }

        if (sender is not FrameworkElement matrixGrid)
        {
            return;
        }

        var point = e.GetCurrentPoint(matrixGrid);
        var properties = point.Properties;

        if ((dragMode == DragMode.Paint && !properties.IsLeftButtonPressed)
            || (dragMode == DragMode.Clear && !properties.IsRightButtonPressed))
        {
            ResetDrag();
            return;
        }

        ExecuteCellCommand(GetCellIndex(point.Position, matrixGrid));
        e.Handled = true;
    }

    private void OnMatrixPointerEnded(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement matrixGrid)
        {
            return;
        }

        ResetDrag();
        matrixGrid.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private static int GetCellIndex(Windows.Foundation.Point position, FrameworkElement matrixGrid)
    {
        if (matrixGrid.ActualWidth <= 0 || matrixGrid.ActualHeight <= 0)
        {
            return -1;
        }

        if (position.X < 0 || position.Y < 0 || position.X >= matrixGrid.ActualWidth || position.Y >= matrixGrid.ActualHeight)
        {
            return -1;
        }

        var column = Math.Min((int)(position.X / (matrixGrid.ActualWidth / 5)), 4);
        var row = Math.Min((int)(position.Y / (matrixGrid.ActualHeight / 5)), 4);
        return (row * 5) + column;
    }

    private void ExecuteCellCommand(int cellIndex)
    {
        if (cellIndex < 0 || !processedDragCells.Add(cellIndex))
        {
            return;
        }

        var command = dragMode == DragMode.Clear
            ? CellRightTappedCommand
            : CellTappedCommand;

        if (command?.CanExecute(cellIndex) == true)
        {
            command.Execute(cellIndex);
        }
    }

    private void ResetDrag()
    {
        dragMode = DragMode.None;
        processedDragCells.Clear();
    }
}