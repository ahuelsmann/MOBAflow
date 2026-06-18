// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Represents the layout state for a single column in a grid layout.
/// </summary>
public class ColumnState
{
    /// <summary>
    /// Gets or sets the width of the column when expanded.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets whether the column is expanded (visible) or collapsed (width = 0).
    /// </summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum width the column can have when expanded.
    /// </summary>
    public double MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum width the column can have when expanded.
    /// NaN means no maximum limit.
    /// </summary>
    public double MaxWidth { get; set; } = double.NaN;

    /// <summary>
    /// Gets or sets the default width for the column when no persisted value exists.
    /// </summary>
    public double DefaultWidth { get; set; }
}

/// <summary>
/// Represents the complete layout configuration for a specific page.
/// </summary>
public class PageLayoutSettings
{
    /// <summary>
    /// Gets or sets the unique key identifying the page.
    /// </summary>
    public string PageKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column configurations for this page.
    /// Key is the column name, value is the column state.
    /// </summary>
    public Dictionary<string, ColumnState> Columns { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when this layout was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents GridSplitter-specific layout settings for persistence.
/// </summary>
public class GridSplitterLayoutSettings
{
    /// <summary>
    /// Gets or sets the column widths by column name.
    /// </summary>
    public Dictionary<string, double> ColumnWidths { get; set; } = new();

    /// <summary>
    /// Gets or sets the expanded/collapsed states by column name.
    /// </summary>
    public Dictionary<string, bool> ColumnExpandedStates { get; set; } = new();

    /// <summary>
    /// Gets or sets the splitter positions by splitter name.
    /// </summary>
    public Dictionary<string, double> SplitterPositions { get; set; } = new();
}