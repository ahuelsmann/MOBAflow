// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.SharedUI.Shell;

/// <summary>
/// Describes a page that can be displayed in the shell.
/// </summary>
public sealed record PageDescriptor
{
    /// <summary>
    /// Unique identifier for the page.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// Display title for navigation.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Fluent icon glyph (optional).
    /// </summary>
    public string? IconGlyph { get; init; }

    /// <summary>
    /// The page type to instantiate.
    /// </summary>
    public required Type PageType { get; init; }

    /// <summary>
    /// Whether this page appears in the main navigation.
    /// </summary>
    public bool ShowInNavigation { get; init; } = true;

    /// <summary>
    /// Navigation group (for grouping in NavigationView).
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Sort order within the navigation.
    /// </summary>
    public int Order { get; init; }
}
