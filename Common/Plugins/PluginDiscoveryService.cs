// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.Logging;

using System.Reflection;

namespace Moba.Common.Plugins;

/// <summary>
/// Discovers and validates plugins in a specified directory.
/// Uses PluginLoadContext for proper plugin isolation per Microsoft Best Practices.
/// See: https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
/// </summary>
public sealed class PluginDiscoveryService
{
    /// <summary>
    /// Discovers all IPlugin implementations in DLL files within the plugin directory.
    /// Each plugin is loaded in its own AssemblyLoadContext for isolation.
    /// </summary>
    /// <param name="pluginDirectory">Path to directory containing plugin DLLs or subdirectories with plugins</param>
    /// <param name="logger">Optional logger for diagnostic information</param>
    /// <returns>List of discovered plugin instances (empty if no plugins found or directory doesn't exist)</returns>
    public static IReadOnlyList<IPlugin> DiscoverPlugins(string pluginDirectory, ILogger? logger = null)
    {
        var plugins = new List<IPlugin>();

        // Handle case where plugin directory doesn't exist
        if (!Directory.Exists(pluginDirectory))
        {
            logger?.LogInformation("Plugin directory does not exist: {PluginDirectory}. Starting without plugins.", pluginDirectory);
            return plugins.AsReadOnly();
        }

        // Try to find plugin DLLs - search in root and immediate subdirectories
        // This supports both layouts: Plugins/*.dll and Plugins/PluginName/PluginName.dll
        var pluginDlls = new List<string>();
        
        // Search root directory
        pluginDlls.AddRange(Directory.EnumerateFiles(pluginDirectory, "*.dll"));
        
        // Search immediate subdirectories (each plugin in its own folder)
        foreach (var subDir in Directory.EnumerateDirectories(pluginDirectory))
        {
            var subDirName = Path.GetFileName(subDir);
            // Look for DLL with same name as directory (convention: PluginName/PluginName.dll)
            var conventionDll = Path.Combine(subDir, $"{subDirName}.dll");
            if (File.Exists(conventionDll))
            {
                pluginDlls.Add(conventionDll);
            }
            else
            {
                // Fallback: add all DLLs in subdirectory
                pluginDlls.AddRange(Directory.EnumerateFiles(subDir, "*.dll"));
            }
        }
        
        if (pluginDlls.Count == 0)
        {
            logger?.LogInformation("No plugin DLLs found in: {PluginDirectory}. Starting without plugins.", pluginDirectory);
            return plugins.AsReadOnly();
        }

        // Load and discover plugins from each DLL using isolated AssemblyLoadContext
        foreach (var dll in pluginDlls)
        {
            try
            {
                var assembly = LoadPluginAssembly(dll, logger);
                if (assembly is null) continue;

                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToList();

                if (!pluginTypes.Any())
                {
                    logger?.LogDebug("No IPlugin implementations found in {Dll}", Path.GetFileName(dll));
                    continue;
                }

                // Instantiate each plugin
                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        if (Activator.CreateInstance(pluginType) is IPlugin plugin)
                        {
                            plugins.Add(plugin);
                            logger?.LogInformation("Discovered plugin: {PluginName} (v{Version}) from {Dll}",
                                plugin.Name, plugin.Metadata.Version ?? "unknown", Path.GetFileName(dll));
                        }
                        else
                        {
                            logger?.LogWarning("Failed to instantiate plugin type {Type} from {Dll}: CreateInstance returned null",
                                pluginType.FullName, Path.GetFileName(dll));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed to instantiate plugin type {Type} from {Dll}",
                            pluginType.FullName, Path.GetFileName(dll));
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to load plugin assembly {Dll}", Path.GetFileName(dll));
            }
        }

        if (plugins.Any())
        {
            logger?.LogInformation("Successfully discovered {PluginCount} plugin(s)", plugins.Count);
        }
        else
        {
            logger?.LogInformation("No plugins were successfully loaded. Starting without plugins.");
        }

        return plugins.AsReadOnly();
    }

    /// <summary>
    /// Loads a plugin assembly using a custom PluginLoadContext for isolation.
    /// This allows plugins to have their own dependencies without conflicting with the host app.
    /// </summary>
    private static Assembly LoadPluginAssembly(string pluginPath, ILogger? logger)
    {
        var pluginLocation = Path.GetFullPath(pluginPath);
        logger?.LogDebug("Loading plugin from: {PluginLocation}", pluginLocation);

        // Create isolated load context for this plugin
        var loadContext = new PluginLoadContext(pluginLocation);

        // Load the plugin assembly by name (not path) through the custom context
        var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation));
        return loadContext.LoadFromAssemblyName(assemblyName);
    }
}