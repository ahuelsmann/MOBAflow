// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Moba.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Moba.WinUI.Service;

/// <summary>
/// Loads and manages plugins from the Plugins directory.
/// Handles plugin discovery, validation, service registration, and lifecycle events.
/// Gracefully handles cases where no plugins are found or directory doesn't exist.
/// </summary>
public sealed class PluginLoader
{
    private readonly string _pluginDirectory;
    private readonly PluginRegistry _registry;
    private readonly List<IPlugin> _loadedPlugins = new();

    public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public PluginLoader(string pluginDirectory, PluginRegistry registry)
    {
        _pluginDirectory = pluginDirectory;
        _registry = registry;
    }

    /// <summary>
    /// Loads all plugins from the plugin directory.
    /// Discovers, validates, and registers plugins with the DI container.
    /// Calls OnInitializedAsync on each plugin after loading is complete.
    /// </summary>
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
    /// Initializes all loaded plugins by calling their OnInitializedAsync method.
    /// Should be called after the application is fully initialized.
    /// </summary>
    public async Task InitializePluginsAsync(ILogger? logger = null)
    {
        if (!_loadedPlugins.Any())
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
    /// Unloads all plugins by calling their OnUnloadingAsync method.
    /// Should be called when the application is shutting down.
    /// </summary>
    public async Task UnloadPluginsAsync(ILogger? logger = null)
    {
        if (!_loadedPlugins.Any())
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
                _registry.AddOrUpdate(page.Tag, page.Title, page.IconGlyph, page.PageType, plugin.Name);
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
