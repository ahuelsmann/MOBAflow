// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Plugins;

using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// Custom AssemblyLoadContext for loading plugins in isolation.
/// Follows Microsoft Best Practices for .NET plugin architecture.
/// See: https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
/// 
/// Key benefits:
/// - Plugins can have their own dependencies without conflicting with host app
/// - Different plugins can use different versions of the same library
/// - Uses AssemblyDependencyResolver for correct dependency resolution via .deps.json
/// - Shared assemblies (Common, SharedUI) are loaded from the host to ensure type identity
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Assemblies that must be shared between host and plugins to ensure type identity.
    /// These are loaded from the default context instead of the plugin's isolated context.
    /// </summary>
    private static readonly HashSet<string> SharedAssemblyNames =
    [
        "Common",
        "SharedUI",
        "Domain",
        "Backend",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "CommunityToolkit.Mvvm",
        "Microsoft.WinUI",
        "Microsoft.Windows.SDK.NET",
        "WinRT.Runtime"
    ];

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
    /// Shared assemblies are loaded from the default context to ensure type identity.
    /// Plugin-specific assemblies are resolved using the plugin's .deps.json file.
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Shared assemblies must come from the default context to ensure type identity
        // This prevents "IPlugin from assembly A is not the same as IPlugin from assembly B" errors
        if (assemblyName.Name is not null && SharedAssemblyNames.Contains(assemblyName.Name))
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }

        // Try to resolve using plugin's dependencies (.deps.json)
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Return null to fall back to default context for framework assemblies
        return null;
    }

    /// <summary>
    /// Resolves and loads native/unmanaged libraries required by the plugin.
    /// </summary>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is not null ? LoadUnmanagedDllFromPath(libraryPath) : nint.Zero;
    }
}
