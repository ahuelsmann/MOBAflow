// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Plugin.Statistics;

using Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Statistics Plugin providing comprehensive project statistics and system status.
/// Shows real-time data about journeys, workflows, trains, Z21 connection, and REST API.
/// 
/// CRITICAL: Plugins must use ContentProvider pattern, NOT Page inheritance!
/// WinUI cannot resolve Page types from dynamically loaded assemblies.
/// </summary>
public sealed class StatisticsPlugin : PluginBase
{
    public override string Name => "Statistics";

    public override PluginMetadata Metadata => new(
        Name,
        Version: "1.0.0",
        Author: "Andreas Huelsmann",
        Description: "Real-time project statistics, connection status, and configuration overview for MOBAflow.",
        MinimumHostVersion: "3.15"
    );

    public override IEnumerable<PluginPageDescriptor> GetPages()
    {
        yield return new PluginPageDescriptor(
            Tag: "statistics",
            Title: "Statistics",
            IconGlyph: "\uE9D2",  // Chart icon
            PageType: typeof(StatisticsPluginContentProvider)
        );
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<StatisticsPluginViewModel>();
        services.AddTransient<StatisticsPluginContentProvider>();
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
