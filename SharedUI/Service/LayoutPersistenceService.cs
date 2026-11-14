// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Common.Configuration;
using Common.Extension;

using Interface;

using Microsoft.Extensions.Logging;

using ViewModel;

/// <summary>
/// The single persistence boundary for the saved layout: page panel expansion, the Settings page sections,
/// column widths and star-column values, and the layout reset. Every change is written to the shared
/// <see cref="AppSettings.Layout"/> under its existing key and persisted through <see cref="ISettingsService"/>.
/// </summary>
public sealed partial class LayoutPersistenceService
{
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ILogger<LayoutPersistenceService> _logger;

    /// <summary>Creates the layout boundary and loads the observable column widths from the saved layout.</summary>
    /// <param name="settingsService">Holds the shared settings and persists every layout change.</param>
    /// <param name="columnWidths">Observable column widths shared by the pages that remember pixel widths.</param>
    /// <param name="logger">Logs layout writes that could not be persisted.</param>
    public LayoutPersistenceService(
        ISettingsService settingsService,
        LayoutColumnWidthsViewModel columnWidths,
        ILogger<LayoutPersistenceService> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(columnWidths);
        ArgumentNullException.ThrowIfNull(logger);
        _settingsService = settingsService;
        _settings = settingsService.GetSettings();
        _logger = logger;
        ColumnWidths = columnWidths;
        ColumnWidths.LoadFrom(_settings.Layout);
    }

    /// <summary>Gets the saved layout. A reset replaces this instance, so callers read it when they need it.</summary>
    public LayoutSettings Layout => _settings.Layout;

    /// <summary>Gets the observable column widths loaded from the saved layout.</summary>
    public LayoutColumnWidthsViewModel ColumnWidths { get; }

    /// <summary>Gets the normalized sort mode of the Settings page sections.</summary>
    public string SettingsSectionSortMode => NormalizeSettingsSectionSortMode(Layout.SettingsPage.SortMode);

    /// <summary>
    /// Applies a layout change and persists it without waiting; a failed write is logged.
    /// </summary>
    /// <param name="apply">Writes the change into the saved layout.</param>
    public void Persist(Action<LayoutSettings> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        apply(Layout);
        SaveAndLog();
    }

    /// <summary>Persists the saved layout and reports a failed write to the caller.</summary>
    public Task SaveAsync() => _settingsService.SaveSettingsAsync(_settings);

    /// <summary>
    /// Replaces every saved layout with its defaults, keeps the tab visibility and persists the result.
    /// A failed write is reported to the caller.
    /// </summary>
    public Task ResetAsync()
    {
        _settings.Layout = new LayoutSettings { TabVisibility = Layout.TabVisibility };
        return SaveAsync();
    }

    /// <summary>
    /// Sets the sort mode of the Settings page sections; an unknown mode falls back to the default and an unchanged
    /// mode is not written.
    /// </summary>
    public void SetSettingsSectionSortMode(string? sortMode)
    {
        var normalized = NormalizeSettingsSectionSortMode(sortMode);
        var layout = Layout.SettingsPage;
        if (layout.SortMode == normalized)
        {
            return;
        }

        layout.SortMode = normalized;
        SaveAndLog();
    }

    /// <summary>Gets the saved state of a Settings page section, creating it on first use.</summary>
    public SettingsPageSectionState GetSettingsSectionState(string sectionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

        var sections = Layout.SettingsPage.Sections;
        if (!sections.TryGetValue(sectionKey, out var state))
        {
            state = new SettingsPageSectionState();
            sections[sectionKey] = state;
        }

        return state;
    }

    /// <summary>Persists whether a Settings page section is expanded; an unchanged value is not written.</summary>
    public void SetSettingsSectionExpanded(string sectionKey, bool isExpanded)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        var state = GetSettingsSectionState(sectionKey);
        if (state.IsExpanded == isExpanded)
        {
            return;
        }

        state.IsExpanded = isExpanded;
        SaveAndLog();
    }

    /// <summary>Counts a use of a Settings page section and remembers when it happened.</summary>
    public void MarkSettingsSectionUsed(string sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        var state = GetSettingsSectionState(sectionKey);
        state.UsageCount++;
        state.LastUsedUtc = DateTimeOffset.UtcNow;
        SaveAndLog();
    }

    /// <summary>Persists the order of the Settings page sections; an unchanged order is not written.</summary>
    public void SetSettingsSectionOrder(IReadOnlyList<string> orderedSectionKeys)
    {
        ArgumentNullException.ThrowIfNull(orderedSectionKeys);

        var changed = false;
        for (var index = 0; index < orderedSectionKeys.Count; index++)
        {
            var sectionKey = orderedSectionKeys[index];
            if (string.IsNullOrWhiteSpace(sectionKey))
            {
                continue;
            }

            var state = GetSettingsSectionState(sectionKey);
            if (state.Order == index)
            {
                continue;
            }

            state.Order = index;
            changed = true;
        }

        if (changed)
        {
            SaveAndLog();
        }
    }

    private void SaveAndLog() => SaveAsync().Observe(ex => LogPersistFailed(_logger, ex));

    private static string NormalizeSettingsSectionSortMode(string? sortMode)
    {
        var trimmed = sortMode?.Trim();
        if (string.Equals(trimmed, SettingsSectionSortModes.Frequency, StringComparison.OrdinalIgnoreCase))
        {
            return SettingsSectionSortModes.Frequency;
        }

        return string.Equals(trimmed, SettingsSectionSortModes.Alphabetical, StringComparison.OrdinalIgnoreCase)
            ? SettingsSectionSortModes.Alphabetical
            : SettingsSectionSortModes.Recent;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Persist layout failed")]
    private static partial void LogPersistFailed(ILogger logger, Exception exception);
}
