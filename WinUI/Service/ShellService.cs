// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.WinUI.Service;

using Moba.SharedUI.Shell;

/// <summary>
/// Manages shell regions and content placement for WinUI.
/// </summary>
public sealed class ShellService : IShellService
{
    private readonly Dictionary<ShellRegion, RegionState> _regions = new();

    /// <inheritdoc />
    public event EventHandler<RegionChangedEventArgs>? RegionChanged;

    /// <inheritdoc />
    public void ShowInRegion(ShellRegion region, object content, RegionDisplayOptions? options = null)
    {
        var state = GetOrCreateRegionState(region);
        state.Content = content;
        state.IsVisible = true;
        state.Options = options;

        RegionChanged?.Invoke(this, new RegionChangedEventArgs
        {
            Region = region,
            IsVisible = true,
            Content = content
        });
    }

    /// <inheritdoc />
    public void ClearRegion(ShellRegion region)
    {
        if (_regions.TryGetValue(region, out var state))
        {
            state.Content = null;
            state.IsVisible = false;

            RegionChanged?.Invoke(this, new RegionChangedEventArgs
            {
                Region = region,
                IsVisible = false,
                Content = null
            });
        }
    }

    /// <inheritdoc />
    public bool IsRegionVisible(ShellRegion region)
    {
        return _regions.TryGetValue(region, out var state) && state.IsVisible;
    }

    /// <inheritdoc />
    public void SetRegionVisibility(ShellRegion region, bool visible)
    {
        var state = GetOrCreateRegionState(region);
        if (state.IsVisible != visible)
        {
            state.IsVisible = visible;

            RegionChanged?.Invoke(this, new RegionChangedEventArgs
            {
                Region = region,
                IsVisible = visible,
                Content = state.Content
            });
        }
    }

    /// <summary>
    /// Gets the content of a region.
    /// </summary>
    public object? GetRegionContent(ShellRegion region)
    {
        return _regions.TryGetValue(region, out var state) ? state.Content : null;
    }

    /// <summary>
    /// Gets the display options for a region.
    /// </summary>
    public RegionDisplayOptions? GetRegionOptions(ShellRegion region)
    {
        return _regions.TryGetValue(region, out var state) ? state.Options : null;
    }

    private RegionState GetOrCreateRegionState(ShellRegion region)
    {
        if (!_regions.TryGetValue(region, out var state))
        {
            state = new RegionState();
            _regions[region] = state;
        }
        return state;
    }

    private sealed class RegionState
    {
        public object? Content { get; set; }
        public bool IsVisible { get; set; }
        public RegionDisplayOptions? Options { get; set; }
    }
}
