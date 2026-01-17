// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.Logging;

using System.Reflection;

namespace Moba.Common.Plugins;

/// <summary>
/// Discovers and loads plugins from a specified directory using isolated assembly loading.
/// </summary>
/// <remarks>
/// <para>
/// This service implements Microsoft's best practices for plugin systems with assembly isolation.
/// Each plugin is loaded in its own <see cref="PluginLoadContext"/> to prevent dependency conflicts.
/// </para>
/// <para>
/// <b>Directory Structure Support:</b>
/// <list type="bullet">
/// <item><description><b>Flat layout:</b> <c>Plugins/*.dll</c> - All plugin DLLs in one directory</description></item>
/// <item><description><b>Nested layout:</b> <c>Plugins/PluginName/PluginName.dll</c> - Each plugin in its own folder</description></item>
/// <item><description><b>Mixed layout:</b> Both flat and nested DLLs are supported simultaneously</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Reference:</b> https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
/// </para>
/// </remarks>
public sealed class PluginDiscoveryService
{
    /// <summary>
    /// Discovers all <see cref="IPlugin"/> implementations in DLL files within the plugin directory.
    /// </summary>
    /// <param name="pluginDirectory">Path to directory containing plugin DLLs or subdirectories with plugins.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>A read-only list of discovered plugin instances. Returns an empty list if no plugins are found or the directory doesn't exist.</returns>
    /// <remarks>
    /// <para>
    /// <b>Discovery Process:</b>
    /// <list type="number">
    /// <item><description>Checks if plugin directory exists (returns empty list if not)</description></item>
    /// <item><description>Scans for DLL files in root directory</description></item>
    /// <item><description>Scans for DLL files in immediate subdirectories</description></item>
    /// <item><description>For each DLL, loads assembly in isolated <see cref="PluginLoadContext"/></description></item>
    /// <item><description>Reflects on assembly to find public, non-abstract <see cref="IPlugin"/> implementations</description></item>
    /// <item><description>Instantiates each plugin type using parameterless constructor</description></item>
    /// <item><description>Returns collection of successfully instantiated plugins</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Error Handling:</b> If a DLL fails to load or a plugin fails to instantiate, that plugin is skipped
    /// and an error is logged. Other plugins continue to load. This ensures partial success is possible.
    /// </para>
    /// <para>
    /// <b>Convention:</b> In nested layout, the service looks for <c>PluginName/PluginName.dll</c> first.
    /// If not found, it scans all DLLs in the subdirectory.
    /// </para>
    /// </remarks>
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

                if (pluginTypes.Count == 0)
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

        if (plugins.Count != 0)
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
    /// Loads a plugin assembly using a custom <see cref="PluginLoadContext"/> for isolation.
    /// </summary>
    /// <param name="pluginPath">The full path to the plugin DLL file.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <returns>The loaded assembly, or <c>null</c> if loading failed.</returns>
    /// <remarks>
    /// <para>
    /// Each plugin is loaded in its own <see cref="PluginLoadContext"/>, which is a custom
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>. This allows plugins to have their own
    /// dependencies without conflicting with the host application or other plugins.
    /// </para>
    /// <para>
    /// The assembly is loaded by name (not by path) through the custom context, ensuring
    /// proper dependency resolution using the plugin's .deps.json file.
    /// </para>
    /// </remarks>
    private static Assembly? LoadPluginAssembly(string pluginPath, ILogger? logger)
    {
        var pluginLocation = Path.GetFullPath(pluginPath);
        logger?.LogDebug("Loading plugin from: {PluginLocation}", pluginLocation);

        try
        {
            // Create isolated load context for this plugin
            var loadContext = new PluginLoadContext(pluginLocation);

            // Load the plugin assembly by name (not by path) through the custom context
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation));
            return loadContext.LoadFromAssemblyName(assemblyName);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create load context or load assembly {PluginPath}", pluginPath);
            return null;
        }
    }
}