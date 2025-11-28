using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Moba.SharedUI.ViewModel;
using MainWindowViewModel = Moba.SharedUI.ViewModel.WinUI.MainWindowViewModel;

namespace Moba.WinUI.View;

public sealed partial class ExplorerPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    private bool _isSplitterDragging;
    private double _splitterStartX;
    private double _leftColumnStartWidth;
    private double _rightColumnStartWidth;

    // For horizontal splitter (TreeView | Properties/Cities)
    private bool _isHorizontalSplitterDragging;
    private double _horizontalSplitterStartX;
    private double _treeViewColumnStartWidth;
    private double _propertiesCitiesColumnStartWidth;

    public ExplorerPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void SolutionTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeNodeViewModel node)
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Node selected: {node.DisplayName}");
            System.Diagnostics.Debug.WriteLine($"   DataContext: {node.DataContext != null}");
            System.Diagnostics.Debug.WriteLine($"   DataType: {node.DataType?.Name}");
            
            ViewModel.OnNodeSelected(node);
            
            System.Diagnostics.Debug.WriteLine($"   Properties Count: {ViewModel.Properties.Count}");
        }
    }

    private async void LoadCitiesButton_Click(object sender, RoutedEventArgs e)
    {
        // Call the async method to load cities from file
        await ViewModel.LoadCitiesFromFileCommand.ExecuteAsync(null);
    }

    // GridSplitter functionality
    private void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Child is Rectangle rectangle)
        {
            rectangle.Opacity = 0.6;
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }
    }

    private void Splitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging && sender is Border border && border.Child is Rectangle rectangle)
        {
            rectangle.Opacity = 0.3;
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow); // Reset cursor
        }
    }

    private void Splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            _isSplitterDragging = true;
            border.CapturePointer(e.Pointer);
            
            if (border.Child is Rectangle rectangle)
                rectangle.Opacity = 1.0;

            // Find the parent Grid (the one with 3 columns: Properties | Splitter | Cities)
            var parentGrid = border.Parent as Grid;
            if (parentGrid != null && parentGrid.ColumnDefinitions.Count >= 3)
            {
                _splitterStartX = e.GetCurrentPoint(border).Position.X;
                
                // Column 0 = Properties, Column 1 = Splitter, Column 2 = Cities
                _leftColumnStartWidth = parentGrid.ColumnDefinitions[0].ActualWidth;
                _rightColumnStartWidth = parentGrid.ColumnDefinitions[2].ActualWidth;
                
                System.Diagnostics.Debug.WriteLine($"🖱️ Splitter drag started:");
                System.Diagnostics.Debug.WriteLine($"   Left (Properties): {_leftColumnStartWidth}px");
                System.Diagnostics.Debug.WriteLine($"   Right (Cities): {_rightColumnStartWidth}px");
            }

            e.Handled = true;
        }
    }

    private void Splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isSplitterDragging && sender is Border border)
        {
            var parentGrid = border.Parent as Grid;
            if (parentGrid != null && parentGrid.ColumnDefinitions.Count >= 3)
            {
                var currentX = e.GetCurrentPoint(border).Position.X;
                var delta = currentX - _splitterStartX;
                
                var leftColumn = parentGrid.ColumnDefinitions[0];
                var rightColumn = parentGrid.ColumnDefinitions[2];

                var newLeftWidth = _leftColumnStartWidth + delta;
                var newRightWidth = _rightColumnStartWidth - delta;

                // Respect Min/Max Width
                var leftMin = leftColumn.MinWidth;
                var rightMin = rightColumn.MinWidth;
                var rightMax = rightColumn.MaxWidth;

                if (newLeftWidth >= leftMin && 
                    newRightWidth >= rightMin && 
                    newRightWidth <= (rightMax > 0 ? rightMax : double.PositiveInfinity))
                {
                    leftColumn.Width = new GridLength(newLeftWidth);
                    rightColumn.Width = new GridLength(newRightWidth);
                    
                    System.Diagnostics.Debug.WriteLine($"↔️ Splitter moved: Delta={delta:F1}px, Left={newLeftWidth:F1}px, Right={newRightWidth:F1}px");
                }
            }

            e.Handled = true;
        }
    }

    private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isSplitterDragging && sender is Border border)
        {
            _isSplitterDragging = false;
            border.ReleasePointerCapture(e.Pointer);
            
            if (border.Child is Rectangle rectangle)
                rectangle.Opacity = 0.3;
            
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow); // Reset cursor
            e.Handled = true;
        }
    }

    // Horizontal Splitter (TreeView | Properties/Cities)
    private void HorizontalSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 0.6;
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }
    }

    private void HorizontalSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isHorizontalSplitterDragging && sender is Border border)
        {
            border.Opacity = 0.3;
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow); // Reset cursor
        }
    }

    private void HorizontalSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && Content is Grid rootGrid)
        {
            _isHorizontalSplitterDragging = true;
            border.CapturePointer(e.Pointer);
            border.Opacity = 1.0;

            // Root grid has: Column 0 = TreeView, Column 1 = Splitter, Column 2 = Properties/Cities
            if (rootGrid.ColumnDefinitions.Count >= 3)
            {
                _horizontalSplitterStartX = e.GetCurrentPoint(border).Position.X;
                _treeViewColumnStartWidth = rootGrid.ColumnDefinitions[0].ActualWidth;
                _propertiesCitiesColumnStartWidth = rootGrid.ColumnDefinitions[2].ActualWidth;
                
                System.Diagnostics.Debug.WriteLine($"🖱️ Horizontal splitter drag started:");
                System.Diagnostics.Debug.WriteLine($"   TreeView: {_treeViewColumnStartWidth}px");
                System.Diagnostics.Debug.WriteLine($"   Properties/Cities: {_propertiesCitiesColumnStartWidth}px");
            }

            e.Handled = true;
        }
    }

    private void HorizontalSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isHorizontalSplitterDragging && sender is Border border && Content is Grid rootGrid)
        {
            if (rootGrid.ColumnDefinitions.Count >= 3)
            {
                var currentX = e.GetCurrentPoint(border).Position.X;
                var delta = currentX - _horizontalSplitterStartX;
                
                var treeViewColumn = rootGrid.ColumnDefinitions[0];
                var propertiesCitiesColumn = rootGrid.ColumnDefinitions[2];

                var newTreeViewWidth = _treeViewColumnStartWidth + delta;
                var newPropertiesCitiesWidth = _propertiesCitiesColumnStartWidth - delta;

                // Respect Min/Max Width
                var treeViewMin = treeViewColumn.MinWidth;
                var treeViewMax = treeViewColumn.MaxWidth;
                var propertiesCitiesMin = propertiesCitiesColumn.MinWidth;

                if (newTreeViewWidth >= treeViewMin && 
                    newTreeViewWidth <= (treeViewMax > 0 ? treeViewMax : double.PositiveInfinity) &&
                    newPropertiesCitiesWidth >= propertiesCitiesMin)
                {
                    treeViewColumn.Width = new GridLength(newTreeViewWidth);
                    propertiesCitiesColumn.Width = new GridLength(newPropertiesCitiesWidth);
                    
                    System.Diagnostics.Debug.WriteLine($"↔️ Horizontal splitter: Delta={delta:F1}px, TreeView={newTreeViewWidth:F1}px, Props/Cities={newPropertiesCitiesWidth:F1}px");
                }
            }

            e.Handled = true;
        }
    }

    private void HorizontalSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isHorizontalSplitterDragging && sender is Border border)
        {
            _isHorizontalSplitterDragging = false;
            border.ReleasePointerCapture(e.Pointer);
            border.Opacity = 0.3;
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow); // Reset cursor
            
            System.Diagnostics.Debug.WriteLine("✅ Horizontal splitter drag ended");
            
            e.Handled = true;
        }
    }
}
