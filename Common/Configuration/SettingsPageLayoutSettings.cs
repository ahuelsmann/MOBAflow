// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

public class SettingsPageLayoutSettings
{
    public string SortMode { get; set; } = SettingsSectionSortModes.Recent;

    public Dictionary<string, SettingsPageSectionState> Sections { get; set; } = new();
}

public class SettingsPageSectionState
{
    public int UsageCount { get; set; }

    public DateTimeOffset? LastUsedUtc { get; set; }

    public bool IsExpanded { get; set; }

    public int Order { get; set; } = int.MaxValue;
}

public static class SettingsSectionSortModes
{
    public const string Recent = "recent";

    public const string Frequency = "frequency";

    public const string Alphabetical = "alphabetical";
}
