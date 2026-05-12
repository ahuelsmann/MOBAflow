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

    public ViewModel5x5 ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            DataContext = value;
        }
    }
    private ViewModel5x5 _viewModel = new();
    private readonly HashSet<int> processedDragCells = [];
    private DragMode currentDragMode = DragMode.None;

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
        DataContext = ViewModel;
    }

    private void OnCellTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && int.TryParse(element.Tag?.ToString(), out var index))
        {
            ExecuteCommand(CellTappedCommand, index);
        }
    }

    private void OnCellRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element && int.TryParse(element.Tag?.ToString(), out var index))
        {
            ExecuteCommand(CellRightTappedCommand, index);
            e.Handled = true;
        }
    }

    private void OnMatrixPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(RootGrid);

        if (point.Properties.IsLeftButtonPressed)
        {
            StartDrag(DragMode.Paint, e);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            StartDrag(DragMode.Clear, e);
        }
    }

    private void OnMatrixPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (currentDragMode == DragMode.None)
        {
            return;
        }

        var point = e.GetCurrentPoint(RootGrid);
        if (currentDragMode == DragMode.Paint && !point.Properties.IsLeftButtonPressed ||
            currentDragMode == DragMode.Clear && !point.Properties.IsRightButtonPressed)
        {
            EndDrag();
            return;
        }

        ApplyDragAt(point.Position.X, point.Position.Y);
        e.Handled = true;
    }

    private void OnMatrixPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        EndDrag();
        e.Handled = true;
    }

    private void OnMatrixPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        EndDrag();
    }

    private void OnMatrixPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        EndDrag();
    }

    private void StartDrag(DragMode mode, PointerRoutedEventArgs e)
    {
        currentDragMode = mode;
        processedDragCells.Clear();
        RootGrid.CapturePointer(e.Pointer);

        var point = e.GetCurrentPoint(RootGrid);
        ApplyDragAt(point.Position.X, point.Position.Y);
        e.Handled = true;
    }

    private void EndDrag()
    {
        currentDragMode = DragMode.None;
        processedDragCells.Clear();
        RootGrid.ReleasePointerCaptures();
    }

    private void ApplyDragAt(double x, double y)
    {
        if (!TryGetCellIndex(x, y, out var index) || !processedDragCells.Add(index))
        {
            return;
        }

        ExecuteCommand(currentDragMode == DragMode.Paint ? CellTappedCommand : CellRightTappedCommand, index);
    }

    private bool TryGetCellIndex(double x, double y, out int index)
    {
        index = -1;

        if (RootGrid.ActualWidth <= 0 ||
            RootGrid.ActualHeight <= 0 ||
            x < 0 ||
            y < 0 ||
            x > RootGrid.ActualWidth ||
            y > RootGrid.ActualHeight)
        {
            return false;
        }

        var column = Math.Clamp((int)(x / RootGrid.ActualWidth * 5), 0, 4);
        var row = Math.Clamp((int)(y / RootGrid.ActualHeight * 5), 0, 4);
        index = row * 5 + column;
        return true;
    }

    private static void ExecuteCommand(ICommand? command, int index)
    {
        if (command?.CanExecute(index) == true)
        {
            command.Execute(index);
        }
    }
}