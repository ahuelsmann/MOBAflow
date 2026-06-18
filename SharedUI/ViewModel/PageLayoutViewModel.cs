// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Common.Configuration;
using Moba.SharedUI.Interop;

using System.ComponentModel;

namespace Moba.SharedUI.ViewModel;

/// <summary>
/// ViewModel for managing page layout with VisualStateManager support and GridSplitter integration.
/// Handles column states, visual state transitions, and persistence of layout configurations.
/// </summary>
public class PageLayoutViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    /// <summary>
    /// Gets the unique key identifying this page.
    /// </summary>
    public string PageKey { get; }

    /// <summary>
    /// Gets the collection of column ViewModels managed by this page layout.
    /// </summary>
    public Dictionary<string, ColumnViewModel> Columns { get; }

    public PageLayoutViewModel(string pageKey, AppSettings settings)
    {
        PageKey = pageKey;
        _settings = settings;
        Columns = new Dictionary<string, ColumnViewModel>();
    }

    /// <summary>
    /// Adds a column to the layout.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <param name="defaultWidth">The default width when no persisted value exists.</param>
    /// <param name="minWidth">The minimum width for the column.</param>
    /// <param name="maxWidth">The maximum width for the column (NaN for no limit).</param>
    public void AddColumn(string name, double defaultWidth, double minWidth = 200, double maxWidth = double.NaN)
    {
        var column = new ColumnViewModel(name, defaultWidth, minWidth, maxWidth);
        Columns[name] = column;
    }

    /// <summary>
    /// Sets the expanded state of a column.
    /// </summary>
    /// <param name="columnName">The name of the column to update.</param>
    /// <param name="isExpanded">Whether the column should be expanded.</param>
    public void SetColumnExpanded(string columnName, bool isExpanded)
    {
        if (Columns.TryGetValue(columnName, out var column))
        {
            column.IsExpanded = isExpanded;
        }
    }

    /// <summary>
    /// Sets the width of a column and persists the change.
    /// </summary>
    /// <param name="columnName">The name of the column to update.</param>
    /// <param name="width">The new width for the column.</param>
    public void SetColumnWidth(string columnName, double width)
    {
        if (Columns.TryGetValue(columnName, out var column))
        {
            column.Width = width;
            PersistLayout();
        }
    }

    /// <summary>
    /// Applies column states to a Grid's ColumnDefinitions.
    /// This method should be called by platform-specific implementations.
    /// </summary>
    /// <param name="grid">The Grid whose columns should be updated.</param>
    public void ApplyToGrid(object? grid)
    {
        if (grid is null || !WinUiGridInterop.TryGetColumnDefinitions(grid, out var columnDefinitions))
            return;

        for (int i = 0; i < columnDefinitions.Count && i < Columns.Count; i++)
        {
            var columnName = GetColumnNameByIndex(i);

            if (columnName != null && Columns.TryGetValue(columnName, out var column))
            {
                var columnDefinition = columnDefinitions[i];
                if (columnDefinition != null)
                    column.ApplyToColumnDefinition(columnDefinition);
            }
        }
    }

    /// <summary>
    /// Updates column states from a Grid's ColumnDefinitions.
    /// This method should be called by platform-specific implementations.
    /// </summary>
    /// <param name="grid">The Grid whose columns should be read.</param>
    public void UpdateFromGrid(object? grid)
    {
        if (grid is null || !WinUiGridInterop.TryGetColumnDefinitions(grid, out var columnDefinitions))
            return;

        for (int i = 0; i < columnDefinitions.Count && i < Columns.Count; i++)
        {
            var columnName = GetColumnNameByIndex(i);

            if (columnName != null && Columns.TryGetValue(columnName, out var column))
            {
                var columnDefinition = columnDefinitions[i];
                if (columnDefinition != null)
                    column.UpdateFromColumnDefinition(columnDefinition);
            }
        }
    }

    /// <summary>
    /// Loads the layout configuration from settings.
    /// </summary>
    public void LoadLayout()
    {
        if (_settings.Layout.PageLayouts.TryGetValue(PageKey, out var pageLayout))
        {
            foreach (var kvp in pageLayout.Columns)
            {
                if (Columns.TryGetValue(kvp.Key, out var column))
                {
                    column.FromColumnState(kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Persists the current layout configuration to settings.
    /// </summary>
    public void PersistLayout()
    {
        var pageLayout = new PageLayoutSettings
        {
            PageKey = PageKey,
            LastModified = DateTime.UtcNow
        };

        foreach (var kvp in Columns)
        {
            pageLayout.Columns[kvp.Key] = kvp.Value.ToColumnState();
        }

        _settings.Layout.PageLayouts[PageKey] = pageLayout;
        // Note: SettingsService.SaveSettingsAsync should be called by the caller
    }

    /// <summary>
    /// Toggles the expanded state of a column.
    /// </summary>
    /// <param name="columnName">The name of the column to toggle.</param>
    public void ToggleColumn(string columnName)
    {
        if (Columns.TryGetValue(columnName, out var column))
        {
            column.IsExpanded = !column.IsExpanded;
            PersistLayout();
        }
    }

    /// <summary>
    /// Gets the current visual state name based on column states.
    /// This can be used by platform-specific implementations to generate VisualStateManager state names.
    /// </summary>
    /// <returns>A state name representing the current column configuration.</returns>
    public string GenerateStateName()
    {
        var expandedColumns = Columns.Where(kvp => kvp.Value.IsExpanded)
                                    .Select(kvp => kvp.Key)
                                    .OrderBy(name => name)
                                    .ToArray();

        if (expandedColumns.Length == 0)
            return "AllCollapsed";

        if (expandedColumns.Length == Columns.Count)
            return "AllExpanded";

        return string.Join("_", expandedColumns);
    }

    /// <summary>
    /// Gets the column name by its index in the grid.
    /// </summary>
    /// <param name="index">The column index.</param>
    /// <returns>The column name, or null if not found.</returns>
    protected virtual string? GetColumnNameByIndex(int index)
    {
        // This should be overridden in derived classes or configured externally
        // For now, use a simple naming convention
        var columnNames = Columns.Keys.OrderBy(x => x).ToArray();
        return index < columnNames.Length ? columnNames[index] : null;
    }

    /// <summary>
    /// Handles property changes from columns to trigger updates.
    /// </summary>
    protected virtual void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ColumnViewModel.IsExpanded) ||
            e.PropertyName == nameof(ColumnViewModel.Width))
        {
            // Platform-specific implementations should handle visual state updates
            PersistLayout();
        }
    }
}