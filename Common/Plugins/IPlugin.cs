// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;

namespace Moba.Common.Plugins;

/// <summary>
/// Base interface for all MOBAflow plugins.
/// Plugins are discovered and loaded dynamically from the Plugins directory.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the display name of this plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets metadata about this plugin (version, author, dependencies, etc.).
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Gets the pages exposed by this plugin.
    /// Each page will be added to the NavigationView menu.
    /// </summary>
    IEnumerable<PluginPageDescriptor> GetPages();

    /// <summary>
    /// Configures services for dependency injection.
    /// Called during application startup to register plugin services.
    /// </summary>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Called after plugin is fully loaded and all services are configured.
    /// Use for initialization, logging, resource loading, etc.
    /// </summary>
    Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when host application is shutting down.
    /// Use for cleanup, saving state, disposing resources, etc.
    /// </summary>
    Task OnUnloadingAsync() => Task.CompletedTask;
}

/// <summary>
/// Metadata about a plugin for versioning, dependency tracking, and validation.
/// </summary>
public sealed record PluginMetadata(
    string Name,
    string? Version = null,
    string? Author = null,
    string? Description = null,
    string? MinimumHostVersion = null,
    IEnumerable<string>? Dependencies = null
);

/// <summary>
/// Descriptor for a single page exposed by a plugin.
/// </summary>
public sealed record PluginPageDescriptor(
    string Tag,
    string Title,
    string? IconGlyph,
    Type PageType
);
