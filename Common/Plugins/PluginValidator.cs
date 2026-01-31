// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Plugins;

using Microsoft.Extensions.Logging;

/// <summary>
/// Validates plugins to ensure they meet contract requirements and don't cause conflicts.
/// </summary>
/// <remarks>
/// <para>
/// The validator performs the following checks:
/// <list type="bullet">
/// <item><description><b>Name validation:</b> Plugins must have a non-empty name</description></item>
/// <item><description><b>Tag uniqueness:</b> Page tags must be unique within a plugin</description></item>
/// <item><description><b>Tag validity:</b> Page tags cannot be null or empty</description></item>
/// <item><description><b>PageType validity:</b> Page types cannot be null</description></item>
/// <item><description><b>Reserved tags:</b> Warns if plugins use reserved tag names (non-fatal)</description></item>
/// </list>
/// </para>
/// <para>
/// Reserved tags that should not be used: overview, solution, journeys, workflows, trains,
/// trackplaneditor, journeymap, monitor, settings. Using these may cause navigation conflicts.
/// </para>
/// </remarks>
public sealed class PluginValidator
{
    /// <summary>
    /// Validates a single plugin for contract compliance and common issues.
    /// </summary>
    /// <param name="plugin">The plugin to validate.</param>
    /// <param name="logger">Optional logger for validation messages.</param>
    /// <returns><c>true</c> if the plugin is valid; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// A plugin is considered invalid if:
    /// <list type="bullet">
    /// <item><description>Name property is null, empty, or whitespace</description></item>
    /// <item><description>Any page has a duplicate tag (case-insensitive)</description></item>
    /// <item><description>Any page has a null, empty, or whitespace tag</description></item>
    /// <item><description>Any page has a null PageType</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Reserved tag usage generates a warning but doesn't invalidate the plugin.
    /// </para>
    /// </remarks>
    public static bool ValidatePlugin(IPlugin plugin, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(plugin.Name))
        {
            logger?.LogError("Plugin has empty Name property");
            return false;
        }

        var pages = plugin.GetPages().ToList();

        // Check for duplicate page tags
        var duplicateTags = pages
            .GroupBy(p => p.Tag, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateTags.Count != 0)
        {
            var tags = string.Join(", ", duplicateTags.Select(g => g.Key));
            logger?.LogError("Plugin {PluginName} has duplicate page tags: {Tags}",
                plugin.Name, tags);
            return false;
        }

        // Check for invalid page tags
        var invalidTags = pages.Where(p => string.IsNullOrWhiteSpace(p.Tag)).ToList();
        if (invalidTags.Count != 0)
        {
            logger?.LogError("Plugin {PluginName} has {Count} page(s) with empty tags",
                plugin.Name, invalidTags.Count);
            return false;
        }

        // Check for null page types
        var nullPageTypes = pages.Where(p => p.PageType == null).ToList();
        if (nullPageTypes.Count != 0)
        {
            logger?.LogError("Plugin {PluginName} has {Count} page(s) with null PageType",
                plugin.Name, nullPageTypes.Count);
            return false;
        }

        // Check for reserved page tags
        var reservedTags = new[] { "overview", "solution", "journeys", "workflows", "trains", 
                                   "trackplaneditor", "journeymap", "monitor", "settings" };
        var conflictingTags = pages
            .Where(p => reservedTags.Contains(p.Tag, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (conflictingTags.Count != 0)
        {
            var tags = string.Join(", ", conflictingTags.Select(p => p.Tag));
            logger?.LogWarning("Plugin {PluginName} uses reserved page tag(s): {Tags}. May cause navigation conflicts.",
                plugin.Name, tags);
            // Not fatal, just a warning
        }

        logger?.LogInformation("Plugin {PluginName} validation passed", plugin.Name);
        return true;
    }

    /// <summary>
    /// Validates multiple plugins.
    /// </summary>
    /// <param name="plugins">The collection of plugins to validate.</param>
    /// <param name="logger">Optional logger for validation messages.</param>
    /// <returns>The count of valid plugins.</returns>
    /// <remarks>
    /// <para>
    /// This method validates each plugin using <see cref="ValidatePlugin"/> and counts how many pass validation.
    /// Invalid plugins are logged but not removed from the collection.
    /// </para>
    /// <para>
    /// Use this method during plugin loading to determine if any plugins are usable.
    /// The caller should decide whether to proceed with invalid plugins or skip them.
    /// </para>
    /// </remarks>
    public static int ValidatePlugins(IEnumerable<IPlugin> plugins, ILogger? logger = null)
    {
        var validCount = 0;
        var pluginList = plugins.ToList();

        logger?.LogInformation("Validating {PluginCount} plugin(s)", pluginList.Count);

        foreach (var plugin in pluginList)
        {
            if (ValidatePlugin(plugin, logger))
            {
                validCount++;
            }
        }

        logger?.LogInformation("Validation complete: {ValidCount}/{TotalCount} plugins are valid",
            validCount, pluginList.Count);

        return validCount;
    }
}
