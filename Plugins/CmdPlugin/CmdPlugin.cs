// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;

using Moba.Common.Plugins;

namespace Moba.Plugin.Cmd;

/// <summary>
/// Command Plugin demonstrating MOBAcmd system - a command-based navigation interface.
/// Shows how to create a plugin with command-style transaction codes for quick navigation.
/// 
/// CRITICAL: Plugins must use ContentProvider pattern, NOT Page inheritance!
/// WinUI cannot resolve Page types from dynamically loaded assemblies.
/// </summary>
public sealed class CmdPlugin : PluginBase
{
    public override string Name => "MOBAcmd System";

    public override PluginMetadata Metadata => new(
        Name,
        Version: "1.0.0",
        Author: "Andreas Huelsmann",
        Description: "Command-style transaction navigation with alphanumeric codes for quick access to MOBAflow pages.",
        MinimumHostVersion: "3.15"
    );

    public override IEnumerable<PluginPageDescriptor> GetPages()
    {
        yield return new PluginPageDescriptor(
            Tag: "cmd",
            Title: "MOBAcmd",
            IconGlyph: "\uE756",  // Keyboard icon
            PageType: typeof(CmdTransactionContentProvider)
        );
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<CmdPluginViewModel>();
        services.AddTransient<CmdTransactionContentProvider>();
    }

    public override async Task OnInitializedAsync()
    {
        await Task.CompletedTask;
    }

    public override async Task OnUnloadingAsync()
    {
        await Task.CompletedTask;
    }
}
