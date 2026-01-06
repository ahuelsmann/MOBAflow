// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.Logging;

namespace Moba.Common.Plugins;

/// <summary>
/// Validates plugins to ensure they meet contract requirements.
/// Checks for duplicate page tags, missing names, and other common issues.
/// </summary>
public sealed class PluginValidator
{
    /// <summary>
    /// Validates a single plugin for common issues.
    /// </summary>
    /// <param name="plugin">Plugin to validate</param>
    /// <param name="logger">Optional logger for validation messages</param>
    /// <returns>True if plugin is valid, false otherwise</returns>
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

        if (duplicateTags.Any())
        {
            var tags = string.Join(", ", duplicateTags.Select(g => g.Key));
            logger?.LogError("Plugin {PluginName} has duplicate page tags: {Tags}",
                plugin.Name, tags);
            return false;
        }

        // Check for invalid page tags
        var invalidTags = pages.Where(p => string.IsNullOrWhiteSpace(p.Tag)).ToList();
        if (invalidTags.Any())
        {
            logger?.LogError("Plugin {PluginName} has {Count} page(s) with empty tags",
                plugin.Name, invalidTags.Count);
            return false;
        }

        // Check for null page types
        var nullPageTypes = pages.Where(p => p.PageType == null).ToList();
        if (nullPageTypes.Any())
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

        if (conflictingTags.Any())
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
    /// <param name="plugins">Plugins to validate</param>
    /// <param name="logger">Optional logger for validation messages</param>
    /// <returns>Count of valid plugins</returns>
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
