// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;

using Moba.Common.Plugins;

namespace Moba.Plugin;

/// <summary>
/// Sample plugin demonstrating the plugin framework.
/// Shows how to create a plugin with code-only UI (no XAML).
/// 
/// CRITICAL: Plugins must use ContentProvider pattern, NOT Page inheritance!
/// WinUI cannot resolve Page types from dynamically loaded assemblies.
/// </summary>
public sealed class SamplePlugin : PluginBase
{
    public override string Name => "Sample Plugin";

    public override PluginMetadata Metadata => new(
        Name,
        Version: "1.0.0",
        Author: "Andreas Huelsmann",
        Description: "A sample plugin demonstrating the MOBAflow plugin framework with project statistics.",
        MinimumHostVersion: "3.15"
    );

    public override IEnumerable<PluginPageDescriptor> GetPages()
    {
        yield return new PluginPageDescriptor(
            Tag: "sampleplugin",
            Title: "Sample Plugin",
            IconGlyph: "\uECCD",
            PageType: typeof(SamplePluginContentProvider)  // ContentProvider, NOT Page!
        );
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        // Register plugin ViewModel and ContentProvider
        services.AddTransient<SamplePluginViewModel>();
        services.AddTransient<SamplePluginContentProvider>();
    }

    public override async Task OnInitializedAsync()
    {
        // Called after plugin is loaded and services are configured
        // Use for initialization, logging, resource loading, etc.
        await Task.CompletedTask;
    }

    public override async Task OnUnloadingAsync()
    {
        // Called when host application is shutting down
        // Use for cleanup, saving state, disposing resources, etc.
        await Task.CompletedTask;
    }
}