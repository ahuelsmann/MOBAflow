// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moba.Common.Plugins;

namespace Moba.WinUI.Service;

/// <summary>
/// Loads and manages plugins from the Plugins directory.
/// </summary>
/// <remarks>
/// <para>
/// The PluginLoader coordinates the entire plugin lifecycle:
/// <list type="number">
/// <item><description><b>Discovery:</b> Scans for plugin DLLs using <see cref="PluginDiscoveryService"/></description></item>
/// <item><description><b>Validation:</b> Validates plugins using <see cref="PluginValidator"/></description></item>
/// <item><description><b>Configuration:</b> Calls <see cref="IPlugin.ConfigureServices"/> for each plugin</description></item>
/// <item><description><b>Registration:</b> Registers plugin pages with the navigation system</description></item>
/// <item><description><b>Initialization:</b> Calls <see cref="IPlugin.OnInitializedAsync"/> after app startup</description></item>
/// <item><description><b>Unloading:</b> Calls <see cref="IPlugin.OnUnloadingAsync"/> during app shutdown</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling:</b> The loader is designed for robustness. If any plugin fails to load,
/// validate, or initialize, that plugin is skipped but the application continues. This ensures
/// the app runs even with no plugins or with some broken plugins.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Plugin loading, initialization, and
/// unloading should only be called from a single thread (typically the UI thread during app lifecycle events).
/// </para>
/// </remarks>
public sealed class PluginLoader
{
    private readonly string _pluginDirectory;
    private readonly NavigationRegistry _registry;
    private readonly List<IPlugin> _loadedPlugins = [];

    /// <summary>
    /// Gets the list of successfully loaded plugins.
    /// </summary>
    /// <value>A read-only collection of loaded <see cref="IPlugin"/> instances.</value>
    public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoader"/> class.
    /// </summary>
    /// <param name="pluginDirectory">The directory path where plugin DLLs are located (e.g., "Plugins/").</param>
    /// <param name="registry">The navigation registry for registering plugin pages.</param>
    public PluginLoader(string pluginDirectory, NavigationRegistry registry)
    {
        _pluginDirectory = pluginDirectory;
        _registry = registry;
    }

    /// <summary>
    /// Loads all plugins from the plugin directory.
    /// </summary>
    /// <param name="services">The service collection to register plugin services with.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A task representing the asynchronous load operation.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// <list type="number">
    /// <item><description>Discovers plugin DLLs in the plugin directory</description></item>
    /// <item><description>Validates discovered plugins (name checks, tag uniqueness)</description></item>
    /// <item><description>Calls <see cref="IPlugin.ConfigureServices"/> for each valid plugin</description></item>
    /// <item><description>Registers plugin pages with the navigation registry</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Important:</b> Call this method during application startup, before building the DI container.
    /// After the container is built, call <see cref="InitializePluginsAsync"/> to complete plugin initialization.
    /// </para>
    /// <para>
    /// If the plugin directory doesn't exist, or no plugins are found, the method completes successfully
    /// and the application runs without plugins.
    /// </para>
    /// </remarks>
    public async Task LoadPluginsAsync(IServiceCollection services, ILogger? logger = null)
    {
        try
        {
            // Step 1: Discover plugins
            var plugins = PluginDiscoveryService.DiscoverPlugins(_pluginDirectory, logger);

            if (!plugins.Any())
            {
                logger?.LogInformation("No plugins to load. Application will run with core features only.");
                return;
            }

            // Step 2: Validate plugins
            var validCount = PluginValidator.ValidatePlugins(plugins, logger);

            if (validCount == 0)
            {
                logger?.LogWarning("No valid plugins found. Application will run with core features only.");
                return;
            }

            // Step 3: Register services and pages
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.ConfigureServices(services);
                    RegisterPages(plugin, services, logger);
                    _loadedPlugins.Add(plugin);
                    logger?.LogInformation("Plugin registered: {PluginName}", plugin.Name);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to configure plugin {PluginName}. Skipping.", plugin.Name);
                }
            }

            logger?.LogInformation("Successfully loaded {PluginCount} plugin(s)", _loadedPlugins.Count);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Plugin loading failed. Starting without plugins.");
        }
    }

    /// <summary>
    /// Initializes all loaded plugins by calling their <see cref="IPlugin.OnInitializedAsync"/> method.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this method after the application is fully initialized and the DI container is built.
    /// This allows plugins to resolve services from the container if needed.
    /// </para>
    /// <para>
    /// If a plugin's <see cref="IPlugin.OnInitializedAsync"/> throws an exception, that exception is logged
    /// but initialization continues for remaining plugins. The application is not affected.
    /// </para>
    /// <para>
    /// If no plugins are loaded, this method completes successfully without any action.
    /// </para>
    /// </remarks>
    public async Task InitializePluginsAsync(ILogger? logger = null)
    {
        if (_loadedPlugins.Count == 0)
        {
            return;
        }

        logger?.LogInformation("Initializing {PluginCount} plugin(s)", _loadedPlugins.Count);

        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                await plugin.OnInitializedAsync();
                logger?.LogInformation("Plugin initialized: {PluginName}", plugin.Name);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Plugin initialization failed: {PluginName}", plugin.Name);
            }
        }
    }

    /// <summary>
    /// Unloads all plugins by calling their <see cref="IPlugin.OnUnloadingAsync"/> method.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A task representing the asynchronous unload operation.</returns>
    /// <remarks>
    /// <para>
    /// Call this method when the application is shutting down to give plugins a chance
    /// to save state, dispose resources, and perform cleanup.
    /// </para>
    /// <para>
    /// This method is best-effort. If a plugin's <see cref="IPlugin.OnUnloadingAsync"/> throws an exception,
    /// the exception is logged but shutdown continues for remaining plugins.
    /// </para>
    /// <para>
    /// After all plugins are unloaded, the internal plugin list is cleared.
    /// </para>
    /// </remarks>
    public async Task UnloadPluginsAsync(ILogger? logger = null)
    {
        if (_loadedPlugins.Count == 0)
        {
            return;
        }

        logger?.LogInformation("Unloading {PluginCount} plugin(s)", _loadedPlugins.Count);

        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                await plugin.OnUnloadingAsync();
                logger?.LogInformation("Plugin unloaded: {PluginName}", plugin.Name);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Plugin unload failed: {PluginName}", plugin.Name);
            }
        }

        _loadedPlugins.Clear();
    }

    /// <summary>
    /// Registers plugin pages with the dependency injection container and navigation registry.
    /// </summary>
    /// <param name="plugin">The plugin whose pages to register.</param>
    /// <param name="services">The service collection to register page types with.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <remarks>
    /// For each page descriptor returned by <see cref="IPlugin.GetPages"/>:
    /// <list type="bullet">
    /// <item><description>Registers the page type with the DI container as Transient</description></item>
    /// <item><description>Registers the page with the navigation registry for menu display</description></item>
    /// </list>
    /// If a page's PageType is null or registration fails, that page is skipped.
    /// </remarks>
    private void RegisterPages(IPlugin plugin, IServiceCollection services, ILogger? logger)
    {
        foreach (var page in plugin.GetPages())
        {
            if (page.PageType is null)
            {
                logger?.LogWarning("Plugin {PluginName} page has null PageType. Skipping.", plugin.Name);
                continue;
            }

            try
            {
                services.AddTransient(page.PageType);
                _registry.Register(page.Tag, page.Title, page.IconGlyph, page.PageType, plugin.Name);
                logger?.LogInformation("Registered plugin page {Tag} ({Title}) from {Plugin}",
                    page.Tag, page.Title, plugin.Name);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to register page {Tag} from plugin {Plugin}",
                    page.Tag, plugin.Name);
            }
        }
    }
}
