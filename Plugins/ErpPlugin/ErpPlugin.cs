// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.DependencyInjection;

using Moba.Common.Plugins;

namespace Moba.Plugin.Erp;

/// <summary>
/// ERP Plugin demonstrating MOBAerp system - a transaction-based navigation interface.
/// Shows how to create a plugin with ERP-style transaction codes for quick navigation.
/// 
/// CRITICAL: Plugins must use ContentProvider pattern, NOT Page inheritance!
/// WinUI cannot resolve Page types from dynamically loaded assemblies.
/// </summary>
public sealed class ErpPlugin : PluginBase
{
    public override string Name => "MOBAerp System";

    public override PluginMetadata Metadata => new(
        Name,
        Version: "1.0.0",
        Author: "Andreas Huelsmann",
        Description: "ERP-style transaction navigation with alphanumeric codes for quick access to MOBAflow pages.",
        MinimumHostVersion: "3.15"
    );

    public override IEnumerable<PluginPageDescriptor> GetPages()
    {
        yield return new PluginPageDescriptor(
            Tag: "erp",
            Title: "MOBAerp",
            IconGlyph: "\uE756",  // Keyboard icon
            PageType: typeof(ErpTransactionContentProvider)
        );
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ErpTransactionContentProvider>();
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
