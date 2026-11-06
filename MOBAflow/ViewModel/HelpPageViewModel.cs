// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Selection state of the help page: which section is shown and its text.
/// </summary>
internal sealed partial class HelpPageViewModel : ObservableObject
{
    /// <summary>Gets the section that is shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedContent))]
    public partial string SelectedSection { get; private set; } = HelpTopics.DefaultSection;

    /// <summary>Gets the help text of <see cref="SelectedSection"/>.</summary>
    public string SelectedContent => HelpTopics.GetContent(SelectedSection);

    /// <summary>Shows <paramref name="section"/>.</summary>
    public void ShowSection(string section)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        SelectedSection = section;
    }
}
