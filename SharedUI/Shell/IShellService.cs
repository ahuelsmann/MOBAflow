// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.SharedUI.Shell;

/// <summary>
/// Manages shell regions and content placement.
/// </summary>
public interface IShellService
{
    /// <summary>
    /// Shows content in the specified shell region.
    /// </summary>
    /// <param name="region">The target region.</param>
    /// <param name="content">The content to display.</param>
    /// <param name="options">Optional display options.</param>
    void ShowInRegion(ShellRegion region, object content, RegionDisplayOptions? options = null);

    /// <summary>
    /// Clears content from the specified region.
    /// </summary>
    /// <param name="region">The region to clear.</param>
    void ClearRegion(ShellRegion region);

    /// <summary>
    /// Gets whether a region is currently visible.
    /// </summary>
    /// <param name="region">The region to check.</param>
    bool IsRegionVisible(ShellRegion region);

    /// <summary>
    /// Sets region visibility.
    /// </summary>
    /// <param name="region">The region to modify.</param>
    /// <param name="visible">Whether the region should be visible.</param>
    void SetRegionVisibility(ShellRegion region, bool visible);

    /// <summary>
    /// Raised when a region's content or visibility changes.
    /// </summary>
    event EventHandler<RegionChangedEventArgs>? RegionChanged;
}

/// <summary>
/// Options for displaying content in a region.
/// </summary>
public sealed class RegionDisplayOptions
{
    /// <summary>
    /// Whether the region should be pinned (stay open).
    /// </summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Initial width for resizable regions (e.g., Sidebar).
    /// </summary>
    public double? InitialWidth { get; init; }

    /// <summary>
    /// Title for the region content.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Icon glyph for the region header.
    /// </summary>
    public string? IconGlyph { get; init; }
}

/// <summary>
/// Event arguments for region changes.
/// </summary>
public sealed class RegionChangedEventArgs : EventArgs
{
    public required ShellRegion Region { get; init; }
    public bool IsVisible { get; init; }
    public object? Content { get; init; }
}
