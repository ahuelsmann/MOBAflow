// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Moba.Backend.Model;

/// <summary>
/// ViewModel for the Settings tab in the Editor.
/// Form editor for Solution Settings (Z21, Speech, Voices, etc.).
/// </summary>
public partial class SettingsEditorViewModel : ObservableObject
{
    private readonly Settings _settings;

    [ObservableProperty]
    private string _speechKey;

    [ObservableProperty]
    private string _speechRegion;

    [ObservableProperty]
    private string _voiceName;

    public SettingsEditorViewModel(Solution solution)
    {
        _settings = solution.Settings;
        _speechKey = _settings.SpeechKey ?? string.Empty;
        _speechRegion = _settings.SpeechRegion ?? string.Empty;
        _voiceName = _settings.VoiceName ?? "de-DE-KatjaNeural";
    }

    partial void OnSpeechKeyChanged(string value)
    {
        _settings.SpeechKey = value;
    }

    partial void OnSpeechRegionChanged(string value)
    {
        _settings.SpeechRegion = value;
    }

    partial void OnVoiceNameChanged(string value)
    {
        _settings.VoiceName = value;
    }
}
