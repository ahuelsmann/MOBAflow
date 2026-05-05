// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

internal sealed partial class SettingsPage
{
    private readonly Dictionary<FrameworkElement, string> _trackedSectionKeys = new();
    private readonly Dictionary<TextBox, string> _trackedTextValues = new();
    private readonly Dictionary<NumberBox, double> _trackedNumberValues = new();
    private readonly Dictionary<string, int> _defaultSectionOrder = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _defaultSectionExpandedStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Expander, long> _expanderExpansionTokens = new();
    private readonly HashSet<FrameworkElement> _trackedControls = [];
    private bool _isSectionLayoutInitialized;
    private bool _isUpdatingSectionLayout;

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isSectionLayoutInitialized)
        {
            return;
        }

        _isUpdatingSectionLayout = true;
        CaptureSectionDefaults();
        SyncSortModeSelection();
        RestoreSectionExpansionStates();
        ApplySectionOrdering();
        _isUpdatingSectionLayout = false;
        _isSectionLayoutInitialized = true;
    }

    private void SettingsSectionSortModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSectionLayout)
        {
            return;
        }

        if (SettingsSectionSortModeComboBox.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag is not string sortMode)
        {
            return;
        }

        ViewModel.SettingsSectionSortMode = sortMode;
        ApplySectionOrdering();
    }

    private void CaptureSectionDefaults()
    {
        foreach (var expander in GetSectionExpanders())
        {
            var sectionKey = GetSectionKey(expander);
            if (string.IsNullOrWhiteSpace(sectionKey))
            {
                continue;
            }

            if (!_defaultSectionOrder.ContainsKey(sectionKey))
            {
                _defaultSectionOrder[sectionKey] = _defaultSectionOrder.Count;
            }

            if (!_defaultSectionExpandedStates.ContainsKey(sectionKey))
            {
                _defaultSectionExpandedStates[sectionKey] = expander.IsExpanded;
            }

            RegisterSectionExpansionTracking(expander);
            RegisterSectionControls(expander.Content as DependencyObject, sectionKey);
        }
    }

    private void SyncSortModeSelection()
    {
        var selectedMode = ViewModel.SettingsSectionSortMode;
        var matchingItem = SettingsSectionSortModeComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, selectedMode, StringComparison.OrdinalIgnoreCase));

        if (matchingItem != null)
        {
            SettingsSectionSortModeComboBox.SelectedItem = matchingItem;
            return;
        }

        SettingsSectionSortModeComboBox.SelectedIndex = 0;
    }

    private void ResetSectionLayoutToDefaults()
    {
        if (!_isSectionLayoutInitialized)
        {
            return;
        }

        _isUpdatingSectionLayout = true;
        try
        {
            foreach (var expander in GetSectionExpanders())
            {
                var sectionKey = GetSectionKey(expander);
                if (_defaultSectionExpandedStates.TryGetValue(sectionKey, out var isExpanded))
                {
                    expander.IsExpanded = isExpanded;
                }
            }

            SyncSortModeSelection();
            ApplySectionOrdering();
        }
        finally
        {
            _isUpdatingSectionLayout = false;
        }
    }

    private void RestoreSectionExpansionStates()
    {
        foreach (var expander in GetSectionExpanders())
        {
            var sectionKey = GetSectionKey(expander);
            expander.IsExpanded = ViewModel.GetSettingsPageSectionIsExpanded(sectionKey);
            if (expander.IsExpanded)
            {
                RegisterSectionControls(expander.Content as DependencyObject, sectionKey);
            }
        }
    }

    private void ApplySectionOrdering()
    {
        var currentExpanders = GetSectionExpanders();
        if (currentExpanders.Count == 0)
        {
            return;
        }

        var orderedExpanders = OrderExpanders(currentExpanders);
        ViewModel.SetSettingsPageSectionOrder(orderedExpanders.Select(GetSectionKey).ToArray());

        if (currentExpanders.SequenceEqual(orderedExpanders))
        {
            return;
        }

        _isUpdatingSectionLayout = true;
        try
        {
            SettingsExpanderHost.Children.Clear();
            foreach (var expander in orderedExpanders)
            {
                SettingsExpanderHost.Children.Add(expander);
            }
        }
        finally
        {
            _isUpdatingSectionLayout = false;
        }
    }

    private List<Expander> OrderExpanders(IReadOnlyList<Expander> currentExpanders)
    {
        return ViewModel.SettingsSectionSortMode switch
        {
            SettingsSectionSortModes.Frequency => currentExpanders
                .OrderByDescending(expander => ViewModel.GetSettingsPageSectionState(GetSectionKey(expander)).UsageCount)
                .ThenByDescending(expander => ViewModel.GetSettingsPageSectionState(GetSectionKey(expander)).LastUsedUtc ?? DateTimeOffset.MinValue)
                .ThenBy(GetSectionOrder)
                .ToList(),
            SettingsSectionSortModes.Alphabetical => currentExpanders
                .OrderBy(expander => expander.Header?.ToString(), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(GetSectionOrder)
                .ToList(),
            _ => currentExpanders
                .OrderByDescending(expander => ViewModel.GetSettingsPageSectionState(GetSectionKey(expander)).LastUsedUtc ?? DateTimeOffset.MinValue)
                .ThenByDescending(expander => ViewModel.GetSettingsPageSectionState(GetSectionKey(expander)).UsageCount)
                .ThenBy(GetSectionOrder)
                .ToList()
        };
    }

    private int GetSectionOrder(Expander expander)
    {
        var sectionKey = GetSectionKey(expander);
        var persistedOrder = ViewModel.GetSettingsPageSectionState(sectionKey).Order;
        if (persistedOrder != int.MaxValue)
        {
            return persistedOrder;
        }

        return _defaultSectionOrder.TryGetValue(sectionKey, out var defaultOrder)
            ? defaultOrder
            : int.MaxValue;
    }

    private void RegisterSectionExpansionTracking(Expander expander)
    {
        if (_expanderExpansionTokens.ContainsKey(expander))
        {
            return;
        }

        _expanderExpansionTokens[expander] = expander.RegisterPropertyChangedCallback(Expander.IsExpandedProperty, OnExpanderIsExpandedChanged);
    }

    private void OnExpanderIsExpandedChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (_isUpdatingSectionLayout || sender is not Expander expander)
        {
            return;
        }

        var sectionKey = GetSectionKey(expander);
        ViewModel.SetSettingsPageSectionIsExpanded(sectionKey, expander.IsExpanded);
        if (expander.IsExpanded)
        {
            RegisterSectionControls(expander.Content as DependencyObject, sectionKey);
        }
    }

    private void RegisterSectionControls(DependencyObject? root, string sectionKey)
    {
        if (root is null || string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        if (root is FrameworkElement element && _trackedControls.Add(element))
        {
            _trackedSectionKeys[element] = sectionKey;
            switch (element)
            {
                case TextBox textBox:
                    textBox.GotFocus += TrackedTextBox_GotFocus;
                    textBox.LostFocus += TrackedTextBox_LostFocus;
                    break;
                case NumberBox numberBox:
                    numberBox.GotFocus += TrackedNumberBox_GotFocus;
                    numberBox.LostFocus += TrackedNumberBox_LostFocus;
                    break;
                case ComboBox comboBox:
                    comboBox.SelectionChanged += TrackedComboBox_SelectionChanged;
                    break;
                case CheckBox checkBox:
                    checkBox.Checked += TrackedCheckBox_Changed;
                    checkBox.Unchecked += TrackedCheckBox_Changed;
                    break;
            }
        }

        foreach (var child in GetChildElements(root))
        {
            RegisterSectionControls(child, sectionKey);
        }
    }

    private static IEnumerable<DependencyObject> GetChildElements(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            yield return VisualTreeHelper.GetChild(parent, index);
        }

        switch (parent)
        {
            case ContentControl contentControl when contentControl.Content is DependencyObject content:
                yield return content;
                break;
            case Border border when border.Child is DependencyObject child:
                yield return child;
                break;
            case Panel panel:
                foreach (var childElement in panel.Children)
                {
                    yield return childElement;
                }
                break;
            case ScrollViewer scrollViewer when scrollViewer.Content is DependencyObject scrollContent:
                yield return scrollContent;
                break;
        }
    }

    private void TrackedTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _trackedTextValues[textBox] = textBox.Text;
        }
    }

    private void TrackedTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingSectionLayout || sender is not TextBox textBox)
        {
            return;
        }

        _trackedTextValues.TryGetValue(textBox, out var previousValue);
        if (!string.Equals(previousValue, textBox.Text, StringComparison.Ordinal))
        {
            RecordSectionUsage(textBox);
            _trackedTextValues[textBox] = textBox.Text;
        }
    }

    private void TrackedNumberBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is NumberBox numberBox)
        {
            _trackedNumberValues[numberBox] = numberBox.Value;
        }
    }

    private void TrackedNumberBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingSectionLayout || sender is not NumberBox numberBox)
        {
            return;
        }

        _trackedNumberValues.TryGetValue(numberBox, out var previousValue);
        if (Math.Abs(previousValue - numberBox.Value) > double.Epsilon)
        {
            RecordSectionUsage(numberBox);
            _trackedNumberValues[numberBox] = numberBox.Value;
        }
    }

    private void TrackedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSectionLayout || sender is not ComboBox comboBox)
        {
            return;
        }

        if (e.AddedItems.Count == 0 && e.RemovedItems.Count == 0)
        {
            return;
        }

        RecordSectionUsage(comboBox);
    }

    private void TrackedCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingSectionLayout || sender is not CheckBox checkBox)
        {
            return;
        }

        RecordSectionUsage(checkBox);
    }

    private void RecordSectionUsage(FrameworkElement element)
    {
        if (!_trackedSectionKeys.TryGetValue(element, out var sectionKey) || string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        RecordSectionUsage(sectionKey);
    }

    private void RecordSettingsSectionUsage(string sectionKey)
    {
        RecordSectionUsage(sectionKey);
    }

    private void RecordSectionUsage(string sectionKey)
    {
        if (_isUpdatingSectionLayout || string.IsNullOrWhiteSpace(sectionKey))
        {
            return;
        }

        ViewModel.MarkSettingsPageSectionUsed(sectionKey);
        if (!string.Equals(ViewModel.SettingsSectionSortMode, SettingsSectionSortModes.Alphabetical, StringComparison.OrdinalIgnoreCase))
        {
            ApplySectionOrdering();
        }
    }

    private List<Expander> GetSectionExpanders()
    {
        return SettingsExpanderHost.Children.OfType<Expander>().ToList();
    }

    private static string GetSectionKey(Expander expander)
    {
        return expander.Header?.ToString() ?? string.Empty;
    }
}
