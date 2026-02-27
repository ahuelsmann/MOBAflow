// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Navigation;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel entry for a feature toggle in the Settings page.
/// Enables dynamic binding to CheckBox for the feature toggle list.
/// </summary>
public sealed partial class FeatureToggleItemViewModel : ObservableObject
{
    /// <summary>
    /// Display text for the CheckBox (page title, optionally with badge).
    /// </summary>
    [ObservableProperty]
    private string _checkBoxContent = string.Empty;

    /// <summary>
    /// Feature toggle switch. Changes are propagated to settings via the setter.
    /// </summary>
    [ObservableProperty]
    private bool _isChecked;

    /// <summary>
    /// Feature toggle key (e.g. IsOverviewPageAvailable).
    /// </summary>
    public string FeatureToggleKey { get; }

    partial void OnIsCheckedChanged(bool value)
    {
        OnIsCheckedChangedCallback?.Invoke(FeatureToggleKey, value);
    }

    /// <summary>
    /// Callback invoked when IsChecked changes.
    /// Parameters: (featureToggleKey, newValue)
    /// </summary>
    internal Action<string, bool>? OnIsCheckedChangedCallback { get; set; }

    public FeatureToggleItemViewModel(FeatureTogglePageInfo info, bool initialValue)
    {
        FeatureToggleKey = info.FeatureToggleKey;
        CheckBoxContent = string.IsNullOrWhiteSpace(info.BadgeLabel)
            ? info.Title
            : $"{info.Title} ({info.BadgeLabel})";
        IsChecked = initialValue;
    }

    /// <summary>
    /// Updates IsChecked (e.g. after Reset to Defaults).
    /// </summary>
    internal void SetChecked(bool value)
    {
        if (IsChecked != value)
        {
            IsChecked = value;
        }
    }
}
