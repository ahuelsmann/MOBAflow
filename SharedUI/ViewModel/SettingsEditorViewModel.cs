// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Moba.Common.Configuration;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Settings tab in the Editor.
/// Form editor for Application Settings (Z21, Speech, Application behavior, etc.).
/// Settings are stored in appsettings.json (not in Solution).
/// </summary>
public partial class SettingsEditorViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    // Z21 Configuration
    [ObservableProperty]
    private string _currentIpAddress;

    [ObservableProperty]
    private ObservableCollection<string> _ipAddressHistory;

    // Azure Speech Configuration
    [ObservableProperty]
    private string _speechKey;

    [ObservableProperty]
    private string _speechRegion;

    [ObservableProperty]
    private int _speechSynthesizerRate;

    [ObservableProperty]
    private uint _speechSynthesizerVolume;

    [ObservableProperty]
    private string _speakerEngineName;

    [ObservableProperty]
    private string _voiceName;

    // Application Settings
    [ObservableProperty]
    private bool _isResetWindowLayoutOnStart;

    public SettingsEditorViewModel(AppSettings settings)
    {
        _settings = settings;
        
        // Initialize Z21 Configuration
        _currentIpAddress = _settings.Z21.CurrentIpAddress;
        _ipAddressHistory = new ObservableCollection<string>(_settings.Z21.RecentIpAddresses);
        
        // Initialize Azure Speech Configuration
        _speechKey = _settings.Speech.Key;
        _speechRegion = _settings.Speech.Region;
        _speechSynthesizerRate = _settings.Speech.Rate;
        _speechSynthesizerVolume = _settings.Speech.Volume;
        _speakerEngineName = _settings.Speech.SpeakerEngineName ?? "AzureCognitiveSpeech";
        _voiceName = _settings.Speech.VoiceName ?? "de-DE-KatjaNeural";
        
        // Initialize Application Settings
        _isResetWindowLayoutOnStart = _settings.Application.ResetWindowLayoutOnStart;
    }

    // Z21 Configuration Change Handlers
    partial void OnCurrentIpAddressChanged(string value)
    {
        _settings.Z21.CurrentIpAddress = value;
        
        // Add to history if not already present
        if (!string.IsNullOrWhiteSpace(value) && !_settings.Z21.RecentIpAddresses.Contains(value))
        {
            _settings.Z21.RecentIpAddresses.Add(value);
            IpAddressHistory.Add(value);
        }
    }

    // Azure Speech Configuration Change Handlers
    partial void OnSpeechKeyChanged(string value)
    {
        _settings.Speech.Key = value;
    }

    partial void OnSpeechRegionChanged(string value)
    {
        _settings.Speech.Region = value;
    }

    partial void OnSpeechSynthesizerRateChanged(int value)
    {
        _settings.Speech.Rate = value;
    }

    partial void OnSpeechSynthesizerVolumeChanged(uint value)
    {
        _settings.Speech.Volume = value;
    }

    partial void OnSpeakerEngineNameChanged(string value)
    {
        _settings.Speech.SpeakerEngineName = value;
    }

    partial void OnVoiceNameChanged(string value)
    {
        _settings.Speech.VoiceName = value;
    }

    // Application Settings Change Handlers
    partial void OnIsResetWindowLayoutOnStartChanged(bool value)
    {
        _settings.Application.ResetWindowLayoutOnStart = value;
    }
}

