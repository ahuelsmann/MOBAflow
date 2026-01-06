// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;

namespace Moba.Common.Plugins;

/// <summary>
/// Base class for plugins, providing default implementations for common plugin functionality.
/// Inherit from this class to simplify plugin development.
/// </summary>
public abstract class PluginBase : IPlugin
{
    /// <summary>
    /// Gets the display name of this plugin.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets metadata about this plugin.
    /// Override to provide version, author, and dependency information.
    /// </summary>
    public virtual PluginMetadata Metadata => new(Name);

    /// <summary>
    /// Gets the pages exposed by this plugin.
    /// Override to return plugin pages.
    /// Default returns no pages.
    /// </summary>
    public virtual IEnumerable<PluginPageDescriptor> GetPages() 
        => Enumerable.Empty<PluginPageDescriptor>();

    /// <summary>
    /// Configures services for dependency injection.
    /// Override to register plugin services.
    /// Default does nothing.
    /// </summary>
    public virtual void ConfigureServices(IServiceCollection services) 
    {
        _ = services; // Suppress unused parameter warning
    }

    /// <summary>
    /// Called after plugin is fully loaded and all services are configured.
    /// Override to perform initialization.
    /// Default does nothing.
    /// </summary>
    public virtual Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when host application is shutting down.
    /// Override to perform cleanup.
    /// Default does nothing.
    /// </summary>
    public virtual Task OnUnloadingAsync() => Task.CompletedTask;
}
