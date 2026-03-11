using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
// using CommunityToolkit.WinUI.Controls.Sizers;
using SharedUI.ViewModel;

namespace Moba.WinUI.Helpers;

/// <summary>
/// Helper class for integrating CommunityToolkit GridSplitter with PageLayoutViewModel.
/// Handles GridSplitter events and updates the layout view model accordingly.
/// </summary>
public static class GridSplitterHelper
{
    /// <summary>
    /// Attaches a GridSplitter to a PageLayoutViewModel for automatic width persistence.
    /// </summary>
    /// <param name="gridSplitter">The GridSplitter control.</param>
    /// <param name="layoutViewModel">The layout view model.</param>
    /// <param name="leftColumnName">Name of the column to the left of the splitter.</param>
    /// <param name="rightColumnName">Name of the column to the right of the splitter.</param>
    public static void AttachToLayoutViewModel(
        CommunityToolkit.WinUI.Controls.Sizers.GridSplitter gridSplitter, 
        PageLayoutViewModel layoutViewModel,
        string leftColumnName,
        string rightColumnName)
    {
        if (gridSplitter == null || layoutViewModel == null) return;

        // Subscribe to GridSplitter drag completed event
        gridSplitter.DragCompleted += (sender, e) =>
        {
            // Update column widths from the actual Grid
            if (gridSplitter.Parent is Grid grid)
            {
                layoutViewModel.UpdateFromGrid(grid);
            }
        };

        // Set up initial state
        if (gridSplitter.Parent is Grid parentGrid)
        {
            layoutViewModel.ApplyToGrid(parentGrid);
        }
    }

    /// <summary>
    /// Creates a GridSplitter with standard configuration for MOBAflow pages.
    /// </summary>
    /// <param name="columnIndex">The column index where the splitter should be placed.</param>
    /// <param name="width">The width of the splitter (default: 16).</param>
    /// <returns>A configured GridSplitter control.</returns>
    public static CommunityToolkit.WinUI.Controls.Sizers.GridSplitter CreateStandardGridSplitter(int columnIndex, double width = 16)
    {
        return new CommunityToolkit.WinUI.Controls.Sizers.GridSplitter
        {
            Grid.Column = columnIndex,
            Width = width,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            ResizeBehavior = GridSplitterResizeBehavior.BasedOnAlignment,
            ResizeDirection = GridSplitterResizeDirection.Auto,
            Cursor = Microsoft.UI.Core.CoreCursorType.SizeWestEast,
            RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform { X = -width / 2 }
        };
    }

    /// <summary>
    /// Configures a Grid with ColumnDefinitions for a typical 5-column layout.
    /// </summary>
    /// <param name="grid">The Grid to configure.</param>
    /// <param name="leftPanelWidth">Default width for the left panel.</param>
    /// <param name="rightPanelWidth">Default width for the right panel.</param>
    public static void ConfigureStandardLayout(Grid grid, double leftPanelWidth = 300, double rightPanelWidth = 250)
    {
        grid.ColumnDefinitions.Clear();
        
        // Column 0: Left Panel (City Library)
        grid.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(leftPanelWidth),
            MinWidth = 200,
            MaxWidth = 500
        });

        // Column 1: Left Splitter
        grid.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(16),
            MinWidth = 16,
            MaxWidth = 16
        });

        // Column 2: Main Content
        grid.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(1, GridUnitType.Star),
            MinWidth = 300
        });

        // Column 3: Right Splitter
        grid.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(16),
            MinWidth = 16,
            MaxWidth = 16
        });

        // Column 4: Right Panel (Workflow Library)
        grid.ColumnDefinitions.Add(new ColumnDefinition 
        { 
            Width = new GridLength(rightPanelWidth),
            MinWidth = 200,
            MaxWidth = 400
        });
    }

    /// <summary>
    /// Binds column visibility to the layout view model.
    /// </summary>
    /// <param name="grid">The Grid containing the columns.</param>
    /// <param name="layoutViewModel">The layout view model.</param>
    public static void BindColumnVisibility(Grid grid, PageLayoutViewModel layoutViewModel)
    {
        if (grid == null || layoutViewModel == null) return;

        // Subscribe to property changes to update column visibility
        foreach (var column in layoutViewModel.Columns.Values)
        {
            column.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ColumnViewModel.IsExpanded))
                {
                    UpdateColumnVisibility(grid, layoutViewModel);
                }
            };
        }

        // Initial visibility update
        UpdateColumnVisibility(grid, layoutViewModel);
    }

    /// <summary>
    /// Updates the visibility of grid columns based on the layout view model state.
    /// </summary>
    private static void UpdateColumnVisibility(Grid grid, PageLayoutViewModel layoutViewModel)
    {
        var columnMappings = new Dictionary<string, int>
        {
            { "CityLibrary", 0 },
            { "Splitter1", 1 },
            { "MainContent", 2 },
            { "Splitter2", 3 },
            { "WorkflowLibrary", 4 }
        };

        foreach (var mapping in columnMappings)
        {
            if (layoutViewModel.Columns.TryGetValue(mapping.Key, out var column))
            {
                if (mapping.Value < grid.ColumnDefinitions.Count)
                {
                    var columnDefinition = grid.ColumnDefinitions[mapping.Value];
                    
                    // Update width based on expanded state
                    if (column.IsExpanded)
                    {
                        if (mapping.Key == "MainContent")
                        {
                            columnDefinition.Width = new GridLength(1, GridUnitType.Star);
                        }
                        else
                        {
                            columnDefinition.Width = new GridLength(column.Width);
                        }
                    }
                    else
                    {
                        columnDefinition.Width = new GridLength(0);
                    }
                }
            }
        }
    }
}
