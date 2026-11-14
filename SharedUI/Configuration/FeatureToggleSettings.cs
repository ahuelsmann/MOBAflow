// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Feature toggle configuration for controlling page visibility.
/// Users can enable or disable pages by editing appsettings.json; pages without an entry use the
/// defaults of <see cref="FeatureToggleRegistry"/>.
/// </summary>
public class FeatureToggleSettings
{
    /// <summary>
    /// Availability and badge label per page, keyed by a <see cref="FeatureTogglePages"/> key.
    /// Loading fills this dictionary; keys are case-sensitive.
    /// </summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, FeatureTogglePageSettings> Pages { get; } = [];

    /// <summary>Enable the visual timeline editor on the Event Manager page.</summary>
    public bool IsEventManagerVisualEditorAvailable { get; set; } = true;
}

/// <summary>
/// Stored toggle state of one page.
/// </summary>
public class FeatureTogglePageSettings
{
    /// <summary>
    /// Gets or sets whether the page is shown in the navigation.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Gets or sets the badge label (e.g. "Preview"). Null uses the registry default; empty shows no badge.
    /// </summary>
    public string? Label { get; set; }
}
