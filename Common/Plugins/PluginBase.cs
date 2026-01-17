// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;

namespace Moba.Common.Plugins;

/// <summary>
/// Base class for plugins, providing default implementations for common plugin functionality.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to simplify plugin development. All <see cref="IPlugin"/> methods have
/// sensible default implementations, so you only need to override what you actually use.
/// </para>
/// <para>
/// Minimum required override: <see cref="Name"/> property.
/// </para>
/// <para>
/// Example minimal plugin:
/// <code>
/// public sealed class MyPlugin : PluginBase
/// {
///     public override string Name => "My Plugin";
/// }
/// </code>
/// </para>
/// <para>
/// Example full-featured plugin:
/// <code>
/// public sealed class MyPlugin : PluginBase
/// {
///     public override string Name => "My Plugin";
///     
///     public override PluginMetadata Metadata => new(
///         Name,
///         Version: "1.0.0",
///         Author: "John Doe",
///         Description: "Does awesome things",
///         MinimumHostVersion: "3.15"
///     );
///     
///     public override IEnumerable&lt;PluginPageDescriptor&gt; GetPages()
///     {
///         yield return new PluginPageDescriptor(
///             Tag: "myplugin",
///             Title: "My Plugin",
///             IconGlyph: "\uE8F1",
///             PageType: typeof(MyPluginPage)
///         );
///     }
///     
///     public override void ConfigureServices(IServiceCollection services)
///     {
///         services.AddTransient&lt;MyPluginViewModel&gt;();
///         services.AddTransient&lt;MyPluginPage&gt;();
///     }
///     
///     public override async Task OnInitializedAsync()
///     {
///         // Initialize plugin resources
///         await LoadConfigurationAsync();
///     }
///     
///     public override async Task OnUnloadingAsync()
///     {
///         // Clean up plugin resources
///         await SaveStateAsync();
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class PluginBase : IPlugin
{
    /// <summary>
    /// Gets the display name of this plugin.
    /// </summary>
    /// <value>The plugin's human-readable name. Must be unique across all plugins.</value>
    /// <remarks>
    /// This is the only required override. The name is displayed in logs and used for identification.
    /// </remarks>
    public abstract string Name { get; }

    /// <summary>
    /// Gets metadata about this plugin.
    /// </summary>
    /// <value>
    /// Plugin metadata including version, author, and dependencies.
    /// Default implementation returns a <see cref="PluginMetadata"/> with only the <see cref="Name"/> populated.
    /// </value>
    /// <remarks>
    /// Override to provide version, author, description, minimum host version, and dependency information.
    /// This metadata is used for documentation and can be used for compatibility checks.
    /// </remarks>
    public virtual PluginMetadata Metadata => new(Name);

    /// <summary>
    /// Gets the pages exposed by this plugin.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="PluginPageDescriptor"/> instances, each representing a page.
    /// Default implementation returns an empty collection.
    /// </returns>
    /// <remarks>
    /// Override to return plugin pages that will be added to the NavigationView menu.
    /// Each page must have a unique tag and a valid WinUI Page type.
    /// </remarks>
    public virtual IEnumerable<PluginPageDescriptor> GetPages()
        => [];

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to register plugin services with.</param>
    /// <remarks>
    /// <para>
    /// Override to register plugin services (ViewModels, Pages, custom services).
    /// Default implementation does nothing.
    /// </para>
    /// <para>
    /// Plugin services can depend on host services such as:
    /// <list type="bullet">
    /// <item><description><c>MainWindowViewModel</c> - Main application state</description></item>
    /// <item><description><c>IZ21</c> - Z21 control station interface</description></item>
    /// <item><description><c>Solution</c> - Current solution model</description></item>
    /// <item><description><c>ISettingsService</c> - Application settings</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// public override void ConfigureServices(IServiceCollection services)
    /// {
    ///     services.AddTransient&lt;MyPluginViewModel&gt;();
    ///     services.AddTransient&lt;MyPluginPage&gt;();
    ///     services.AddSingleton&lt;IMyService, MyService&gt;();
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public virtual void ConfigureServices(IServiceCollection services)
    {
        _ = services; // Suppress unused parameter warning
    }

    /// <summary>
    /// Called after plugin is fully loaded and all services are configured.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <remarks>
    /// <para>
    /// Override to perform plugin initialization such as:
    /// <list type="bullet">
    /// <item><description>Loading configuration files</description></item>
    /// <item><description>Initializing resources</description></item>
    /// <item><description>Registering event handlers</description></item>
    /// <item><description>Logging plugin startup</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method is called after <see cref="ConfigureServices"/> and after the DI container is built,
    /// so you can resolve services if needed. However, prefer constructor injection in your services.
    /// </para>
    /// <para>
    /// If this method throws an exception, the plugin is marked as failed but the application continues.
    /// The exception is logged automatically.
    /// </para>
    /// <para>
    /// Default implementation does nothing and returns a completed task.
    /// </para>
    /// </remarks>
    public virtual Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when host application is shutting down.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    /// <remarks>
    /// <para>
    /// Override to perform plugin cleanup such as:
    /// <list type="bullet">
    /// <item><description>Saving plugin state</description></item>
    /// <item><description>Disposing resources</description></item>
    /// <item><description>Unregistering event handlers</description></item>
    /// <item><description>Logging plugin shutdown</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method is best-effort. If it throws an exception, the exception is logged
    /// but application shutdown continues. Don't rely on this method for critical cleanup.
    /// </para>
    /// <para>
    /// Default implementation does nothing and returns a completed task.
    /// </para>
    /// </remarks>
    public virtual Task OnUnloadingAsync() => Task.CompletedTask;
}
