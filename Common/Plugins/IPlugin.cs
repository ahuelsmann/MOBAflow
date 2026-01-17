// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;

namespace Moba.Common.Plugins;

/// <summary>
/// Base interface for all MOBAflow plugins.
/// </summary>
/// <remarks>
/// <para>
/// Plugins are discovered and loaded dynamically from the <c>Plugins/</c> directory at application startup.
/// Each plugin DLL is scanned for public classes implementing <see cref="IPlugin"/>.
/// </para>
/// <para>
/// Plugin lifecycle: Discovery → Validation → Configuration → Initialization → Runtime → Unloading
/// </para>
/// <para>
/// Example implementation:
/// <code>
/// public class MyPlugin : PluginBase
/// {
///     public override string Name => "My Plugin";
///     
///     public override IEnumerable&lt;PluginPageDescriptor&gt; GetPages()
///     {
///         yield return new PluginPageDescriptor("mytag", "My Page", "\uE8F1", typeof(MyPage));
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IPlugin
{
    /// <summary>
    /// Gets the display name of this plugin.
    /// </summary>
    /// <value>The plugin's human-readable name. Must be unique across all plugins.</value>
    string Name { get; }

    /// <summary>
    /// Gets metadata about this plugin (version, author, dependencies, etc.).
    /// </summary>
    /// <value>Plugin metadata for versioning and dependency tracking.</value>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Gets the pages exposed by this plugin.
    /// </summary>
    /// <returns>
    /// A collection of page descriptors. Each page will be added to the NavigationView menu.
    /// Return an empty collection if the plugin has no UI pages.
    /// </returns>
    IEnumerable<PluginPageDescriptor> GetPages();

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to register plugin services with.</param>
    /// <remarks>
    /// Called during application startup to register plugin services.
    /// Register ViewModels, Pages, and other services here.
    /// Plugin services can depend on host services (e.g., MainWindowViewModel, IZ21).
    /// </remarks>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Called after plugin is fully loaded and all services are configured.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <remarks>
    /// Use for initialization, logging, resource loading, etc.
    /// This method is called after <see cref="ConfigureServices"/> and after the DI container is built.
    /// If this method throws, the plugin is marked as failed but the application continues.
    /// </remarks>
    Task OnInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when host application is shutting down.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    /// <remarks>
    /// Use for cleanup, saving state, disposing resources, etc.
    /// This method is best-effort; if it throws, the exception is logged but shutdown continues.
    /// </remarks>
    Task OnUnloadingAsync() => Task.CompletedTask;
}

/// <summary>
/// Metadata about a plugin for versioning, dependency tracking, and validation.
/// </summary>
/// <param name="Name">The plugin name (must match <see cref="IPlugin.Name"/>).</param>
/// <param name="Version">The plugin version in semantic versioning format (e.g., "1.0.0"). Optional.</param>
/// <param name="Author">The plugin author or organization. Optional.</param>
/// <param name="Description">A brief description of the plugin's functionality. Optional.</param>
/// <param name="MinimumHostVersion">The minimum required host application version. Optional.</param>
/// <param name="Dependencies">A list of required dependencies (NuGet packages or other plugins). Optional.</param>
/// <remarks>
/// This record is used for version compatibility checks and dependency tracking.
/// It is not enforced at runtime but can be used for validation and documentation.
/// </remarks>
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
/// <param name="Tag">Unique identifier for the page (used for navigation). Must be unique across all plugins.</param>
/// <param name="Title">Display text shown in the NavigationView menu.</param>
/// <param name="IconGlyph">Fluent UI icon glyph (e.g., "\uE8F1"). Optional.</param>
/// <param name="PageType">The Type of the WinUI Page class. Must inherit from <c>Microsoft.UI.Xaml.Controls.Page</c>.</param>
/// <remarks>
/// <para>
/// Reserved tags that cannot be used: overview, solution, journeys, workflows, trains, trackplaneditor, journeymap, monitor, settings.
/// </para>
/// <para>
/// Icon glyphs can be found at: https://learn.microsoft.com/windows/apps/design/style/segoe-fluent-icons-font
/// </para>
/// </remarks>
public sealed record PluginPageDescriptor(
    string Tag,
    string Title,
    string? IconGlyph,
    Type PageType
);
