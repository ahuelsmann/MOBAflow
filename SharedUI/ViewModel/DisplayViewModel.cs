// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

public sealed partial class DisplayViewModel : ObservableObject
{
    public DisplayViewModel()
    {
        WaveshareLcdTouch700InchConfiguration = new DisplayConfigurationViewModel(
            DisplayConfigurationKind.WaveshareLcdTouch700Inch,
            "Waveshare LCD Touch 7.00inch",
            "800 x 480");
        WaveshareLcd169InchConfiguration = new DisplayConfigurationViewModel(
            DisplayConfigurationKind.WaveshareLcd169Inch,
            "Waveshare LCD Module 1.69inch",
            "240 x 280");
        WaveshareLcd147InchConfiguration = new DisplayConfigurationViewModel(
            DisplayConfigurationKind.WaveshareLcd147Inch,
            "Waveshare LCD Module 1.47inch",
            "172 x 320");

        selectedConfiguration = WaveshareLcdTouch700InchConfiguration;
    }

    public DisplayConfigurationViewModel WaveshareLcdTouch700InchConfiguration { get; }

    public DisplayConfigurationViewModel WaveshareLcd169InchConfiguration { get; }

    public DisplayConfigurationViewModel WaveshareLcd147InchConfiguration { get; }

    [ObservableProperty]
    private DisplayConfigurationViewModel selectedConfiguration;

    public bool IsWaveshareLcdTouch700InchSelected =>
        SelectedConfiguration.Kind == DisplayConfigurationKind.WaveshareLcdTouch700Inch;

    public bool IsWaveshareLcd169InchSelected =>
        SelectedConfiguration.Kind == DisplayConfigurationKind.WaveshareLcd169Inch;

    public bool IsWaveshareLcd147InchSelected =>
        SelectedConfiguration.Kind == DisplayConfigurationKind.WaveshareLcd147Inch;

    public void SelectConfiguration(DisplayConfigurationKind kind)
    {
        SelectedConfiguration = kind switch
        {
            DisplayConfigurationKind.WaveshareLcd169Inch => WaveshareLcd169InchConfiguration,
            DisplayConfigurationKind.WaveshareLcd147Inch => WaveshareLcd147InchConfiguration,
            _ => WaveshareLcdTouch700InchConfiguration,
        };
    }

    partial void OnSelectedConfigurationChanged(DisplayConfigurationViewModel value)
    {
        OnPropertyChanged(nameof(IsWaveshareLcdTouch700InchSelected));
        OnPropertyChanged(nameof(IsWaveshareLcd169InchSelected));
        OnPropertyChanged(nameof(IsWaveshareLcd147InchSelected));
    }
}

public enum DisplayConfigurationKind
{
    WaveshareLcdTouch700Inch,
    WaveshareLcd169Inch,
    WaveshareLcd147Inch,
}

public sealed class DisplayConfigurationViewModel
{
    public DisplayConfigurationViewModel(DisplayConfigurationKind kind, string title, string resolution)
    {
        Kind = kind;
        Title = title;
        Resolution = resolution;
    }

    public DisplayConfigurationKind Kind { get; }

    public string Title { get; }

    public string Resolution { get; }

    public string ResolutionLabel => $"Resolution: {Resolution}";
}
