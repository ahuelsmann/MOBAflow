// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Reflection;
using System.Runtime.Loader;

namespace Moba.Common.Plugins;

/// <summary>
/// Custom AssemblyLoadContext for loading plugins in isolation.
/// Follows Microsoft Best Practices for .NET plugin architecture.
/// See: https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
/// 
/// Key benefits:
/// - Plugins can have their own dependencies without conflicting with host app
/// - Different plugins can use different versions of the same library
/// - Uses AssemblyDependencyResolver for correct dependency resolution via .deps.json
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Creates a new PluginLoadContext for loading a plugin from the specified path.
    /// </summary>
    /// <param name="pluginPath">Full path to the plugin DLL file</param>
    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <summary>
    /// Resolves and loads managed assemblies required by the plugin.
    /// First tries to resolve using the plugin's .deps.json file,
    /// then falls back to the default load context.
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve using plugin's dependencies
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Return null to fall back to default context (for shared assemblies like Common, SharedUI)
        return null;
    }

    /// <summary>
    /// Resolves and loads native/unmanaged libraries required by the plugin.
    /// </summary>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath is not null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }
}
