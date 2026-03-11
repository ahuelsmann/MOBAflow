namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

public partial class MainWindowViewModel
{
    public string SettingsSectionSortMode
    {
        get => NormalizeSettingsSectionSortMode(GetSettingsPageLayout().SortMode);
        set
        {
            var normalized = NormalizeSettingsSectionSortMode(value);
            var layout = GetSettingsPageLayout();
            if (layout.SortMode == normalized)
            {
                return;
            }

            layout.SortMode = normalized;
            OnPropertyChanged();
            _ = _settingsService?.SaveSettingsAsync(_settings);
        }
    }

    public SettingsPageSectionState GetSettingsPageSectionState(string sectionKey)
    {
        return GetOrCreateSettingsPageSectionState(sectionKey);
    }

    public bool GetSettingsPageSectionIsExpanded(string sectionKey)
    {
        return GetOrCreateSettingsPageSectionState(sectionKey).IsExpanded;
    }

    public void SetSettingsPageSectionIsExpanded(string sectionKey, bool isExpanded)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        var state = GetOrCreateSettingsPageSectionState(sectionKey);
        if (state.IsExpanded == isExpanded)
        {
            return;
        }

        state.IsExpanded = isExpanded;
        _ = _settingsService?.SaveSettingsAsync(_settings);
    }

    public void MarkSettingsPageSectionUsed(string sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        var state = GetOrCreateSettingsPageSectionState(sectionKey);
        state.UsageCount++;
        state.LastUsedUtc = DateTimeOffset.UtcNow;
        _ = _settingsService?.SaveSettingsAsync(_settings);
    }

    public void SetSettingsPageSectionOrder(IReadOnlyList<string> orderedSectionKeys)
    {
        if (orderedSectionKeys.Count == 0)
        {
            return;
        }

        var changed = false;
        for (var index = 0; index < orderedSectionKeys.Count; index++)
        {
            var sectionKey = orderedSectionKeys[index];
            if (string.IsNullOrWhiteSpace(sectionKey))
            {
                continue;
            }

            var state = GetOrCreateSettingsPageSectionState(sectionKey);
            if (state.Order == index)
            {
                continue;
            }

            state.Order = index;
            changed = true;
        }

        if (changed)
        {
            _ = _settingsService?.SaveSettingsAsync(_settings);
        }
    }

    private SettingsPageLayoutSettings GetSettingsPageLayout()
    {
        return _settings.Layout.SettingsPage;
    }

    private SettingsPageSectionState GetOrCreateSettingsPageSectionState(string sectionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionKey);

        var layout = GetSettingsPageLayout();
        if (!layout.Sections.TryGetValue(sectionKey, out var state) || state == null)
        {
            state = new SettingsPageSectionState();
            layout.Sections[sectionKey] = state;
        }

        return state;
    }

    private static string NormalizeSettingsSectionSortMode(string? sortMode)
    {
        return sortMode?.Trim().ToLowerInvariant() switch
        {
            SettingsSectionSortModes.Frequency => SettingsSectionSortModes.Frequency,
            SettingsSectionSortModes.Alphabetical => SettingsSectionSortModes.Alphabetical,
            _ => SettingsSectionSortModes.Recent
        };
    }
}
