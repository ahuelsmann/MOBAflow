// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.SharedUI.Shell;

/// <summary>
/// Factory for creating pages with dependency injection support.
/// </summary>
public interface IPageFactory
{
    /// <summary>
    /// Creates a page instance by its registered tag.
    /// </summary>
    /// <param name="pageTag">The unique tag identifying the page type.</param>
    /// <returns>The created page instance, or null if not found.</returns>
    object? CreatePage(string pageTag);

    /// <summary>
    /// Creates a page instance of the specified type.
    /// </summary>
    /// <typeparam name="TPage">The page type to create.</typeparam>
    /// <returns>The created page instance.</returns>
    TPage CreatePage<TPage>() where TPage : class;

    /// <summary>
    /// Registers a page type with a tag for later creation.
    /// </summary>
    /// <param name="pageTag">The unique tag for the page.</param>
    /// <param name="pageType">The page type to register.</param>
    void RegisterPage(string pageTag, Type pageType);

    /// <summary>
    /// Gets all registered page tags.
    /// </summary>
    IEnumerable<string> RegisteredPageTags { get; }
}
