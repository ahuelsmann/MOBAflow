// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.SharedUI.Shell;

/// <summary>
/// Provides navigation capabilities within the application shell.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current page tag.
    /// </summary>
    string? CurrentPageTag { get; }

    /// <summary>
    /// Navigates to a page by its tag.
    /// </summary>
    /// <param name="pageTag">The unique tag identifying the page.</param>
    /// <param name="parameter">Optional navigation parameter.</param>
    /// <returns>True if navigation succeeded.</returns>
    bool NavigateTo(string pageTag, object? parameter = null);

    /// <summary>
    /// Navigates back to the previous page if possible.
    /// </summary>
    /// <returns>True if back navigation succeeded.</returns>
    bool GoBack();

    /// <summary>
    /// Gets whether back navigation is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Raised when navigation occurs.
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;
}

/// <summary>
/// Event arguments for navigation events.
/// </summary>
public sealed class NavigationEventArgs : EventArgs
{
    public required string PageTag { get; init; }
    public object? Parameter { get; init; }
    public string? PreviousPageTag { get; init; }
}
