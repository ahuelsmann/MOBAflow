using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using SharedUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MOBAflow.Helpers;

/// <summary>
/// Manages GridSplitter controls and their integration with PageLayoutViewModel.
/// Handles splitter registration, drag events, and column width updates.
/// </summary>
public class GridSplitterLayoutManager
{
    private readonly PageLayoutViewModel _layoutViewModel;
    private readonly Dictionary<string, GridSplitter> _splitters = new();
    private readonly Dictionary<string, int> _splitterColumnMappings = new();

    /// <summary>
    /// Gets the layout ViewModel managed by this manager.
    /// </summary>
    public PageLayoutViewModel LayoutViewModel => _layoutViewModel;

    /// <summary>
    /// Initializes a new instance of the GridSplitterLayoutManager.
    /// </summary>
    /// <param name="layoutViewModel">The PageLayoutViewModel to manage.</param>
    public GridSplitterLayoutManager(PageLayoutViewModel layoutViewModel)
    {
        _layoutViewModel = layoutViewModel ?? throw new ArgumentNullException(nameof(layoutViewModel));
    }

    /// <summary>
    /// Registers a GridSplitter with the manager.
    /// </summary>
    /// <param name="name">The unique name for the splitter.</param>
    /// <param name="splitter">The GridSplitter control to register.</param>
    /// <param name="leftColumnIndex">The index of the column to the left of the splitter.</param>
    /// <param name="rightColumnIndex">The index of the column to the right of the splitter.</param>
    public void RegisterSplitter(string name, GridSplitter splitter, int leftColumnIndex, int rightColumnIndex)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Splitter name cannot be null or empty", nameof(name));
        
        if (splitter == null)
            throw new ArgumentNullException(nameof(splitter));

        _splitters[name] = splitter;
        _splitterColumnMappings[name] = (leftColumnIndex, rightColumnIndex);
        
        splitter.DragCompleted += OnSplitterDragCompleted;
        splitter.DragStarted += OnSplitterDragStarted;
    }

    /// <summary>
    /// Unregisters a GridSplitter from the manager.
    /// </summary>
    /// <param name="name">The name of the splitter to unregister.</param>
    public void UnregisterSplitter(string name)
    {
        if (_splitters.TryGetValue(name, out var splitter))
        {
            splitter.DragCompleted -= OnSplitterDragCompleted;
            splitter.DragStarted -= OnSplitterDragStarted;
            _splitters.Remove(name);
            _splitterColumnMappings.Remove(name);
        }
    }

    /// <summary>
    /// Gets a registered splitter by name.
    /// </summary>
    /// <param name="name">The name of the splitter.</param>
    /// <returns>The GridSplitter if found, otherwise null.</returns>
    public GridSplitter? GetSplitter(string name)
    {
        return _splitters.TryGetValue(name, out var splitter) ? splitter : null;
    }

    /// <summary>
    /// Updates column widths based on current splitter positions.
    /// </summary>
    public void UpdateColumnWidthsFromSplitters()
    {
        foreach (var kvp in _splitterColumnMappings)
        {
            var splitterName = kvp.Key;
            var (leftIndex, rightIndex) = kvp.Value;
            
            if (!_splitters.TryGetValue(splitterName, out var splitter))
                continue;

            UpdateColumnWidthsFromSplitter(splitterName, splitter, leftIndex, rightIndex);
        }
    }

    /// <summary>
    /// Applies column width changes to the associated Grid.
    /// </summary>
    /// <param name="grid">The Grid to update.</param>
    public void ApplyColumnWidthsToGrid(Grid grid)
    {
        if (grid == null) return;

        var columnNames = _layoutViewModel.Columns.Keys.OrderBy(x => x).ToArray();
        
        for (int i = 0; i < grid.ColumnDefinitions.Count && i < columnNames.Length; i++)
        {
            var columnName = columnNames[i];
            if (_layoutViewModel.Columns.TryGetValue(columnName, out var column))
            {
                column.ApplyToColumnDefinition(grid.ColumnDefinitions[i]);
            }
        }
    }

    /// <summary>
    /// Updates column states from the associated Grid.
    /// </summary>
    /// <param name="grid">The Grid to read states from.</param>
    public void UpdateColumnStatesFromGrid(Grid grid)
    {
        if (grid == null) return;

        var columnNames = _layoutViewModel.Columns.Keys.OrderBy(x => x).ToArray();
        
        for (int i = 0; i < grid.ColumnDefinitions.Count && i < columnNames.Length; i++)
        {
            var columnName = columnNames[i];
            if (_layoutViewModel.Columns.TryGetValue(columnName, out var column))
            {
                column.UpdateFromColumnDefinition(grid.ColumnDefinitions[i]);
            }
        }
    }

    /// <summary>
    /// Handles the DragStarted event of a GridSplitter.
    /// </summary>
    private void OnSplitterDragStarted(object? sender, DragStartedEventArgs e)
    {
        // Could be used for visual feedback during drag operations
    }

    /// <summary>
    /// Handles the DragCompleted event of a GridSplitter.
    /// </summary>
    private void OnSplitterDragCompleted(object? sender, DragCompletedEventArgs e)
    {
        if (sender is GridSplitter splitter)
        {
            var splitterName = _splitters.FirstOrDefault(x => x.Value == splitter).Key;
            if (!string.IsNullOrEmpty(splitterName) && _splitterColumnMappings.TryGetValue(splitterName, out var mapping))
            {
                var (leftIndex, rightIndex) = mapping;
                UpdateColumnWidthsFromSplitter(splitterName, splitter, leftIndex, rightIndex);
            }
        }
    }

    /// <summary>
    /// Updates column widths based on a specific splitter's position.
    /// </summary>
    /// <param name="splitterName">The name of the splitter.</param>
    /// <param name="splitter">The GridSplitter control.</param>
    /// <param name="leftColumnIndex">Index of the left column.</param>
    /// <param name="rightColumnIndex">Index of the right column.</param>
    private void UpdateColumnWidthsFromSplitter(string splitterName, GridSplitter splitter, int leftColumnIndex, int rightColumnIndex)
    {
        var columnNames = _layoutViewModel.Columns.Keys.OrderBy(x => x).ToArray();
        
        if (leftColumnIndex >= 0 && leftColumnIndex < columnNames.Length)
        {
            var leftColumnName = columnNames[leftColumnIndex];
            if (_layoutViewModel.Columns.TryGetValue(leftColumnName, out var leftColumn))
            {
                // Calculate the new width based on the splitter position
                // This is a simplified calculation - in a real implementation, you'd need to
                // calculate based on the actual grid layout and splitter position
                var newWidth = CalculateColumnWidth(leftColumn, splitter, leftColumnIndex);
                leftColumn.Width = newWidth;
            }
        }

        if (rightColumnIndex >= 0 && rightColumnIndex < columnNames.Length)
        {
            var rightColumnName = columnNames[rightColumnIndex];
            if (_layoutViewModel.Columns.TryGetValue(rightColumnName, out var rightColumn))
            {
                // Similar calculation for the right column
                var newWidth = CalculateColumnWidth(rightColumn, splitter, rightColumnIndex);
                rightColumn.Width = newWidth;
            }
        }

        // Persist the changes
        _layoutViewModel.PersistLayout();
    }

    /// <summary>
    /// Calculates the new width for a column based on splitter position.
    /// </summary>
    /// <param name="column">The column to update.</param>
    /// <param name="splitter">The splitter control.</param>
    /// <param name="columnIndex">The index of the column.</param>
    /// <returns>The calculated new width.</returns>
    private double CalculateColumnWidth(ColumnViewModel column, GridSplitter splitter, int columnIndex)
    {
        // This is a simplified implementation
        // In a real scenario, you'd need to:
        // 1. Get the actual position of the splitter
        // 2. Calculate the width based on the grid's layout
        // 3. Respect MinWidth and MaxWidth constraints
        
        // For now, return the current width + a small delta (simulating drag)
        var delta = splitter.HorizontalAlignment == HorizontalAlignment.Left ? 5 : -5;
        var newWidth = column.Width + delta;
        
        // Apply constraints
        if (newWidth < column.MinWidth) newWidth = column.MinWidth;
        if (column.MaxWidth > 0 && !double.IsNaN(column.MaxWidth) && newWidth > column.MaxWidth)
            newWidth = column.MaxWidth;
        
        return newWidth;
    }

    /// <summary>
    /// Disposes all resources and event handlers.
    /// </summary>
    public void Dispose()
    {
        foreach (var splitter in _splitters.Values)
        {
            splitter.DragCompleted -= OnSplitterDragCompleted;
            splitter.DragStarted -= OnSplitterDragStarted;
        }
        
        _splitters.Clear();
        _splitterColumnMappings.Clear();
    }
}
