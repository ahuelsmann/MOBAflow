// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Moba.Domain;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Settings tab in the Editor.
/// Form editor for Solution Settings (Z21, Speech, Application behavior, etc.).
/// </summary>
public partial class SettingsEditorViewModel : ObservableObject
{
    private readonly Settings _settings;

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

    public SettingsEditorViewModel(Settings settings)
    {
        _settings = settings;
        
        // Initialize Z21 Configuration
        _currentIpAddress = _settings.CurrentIpAddress ?? "192.168.0.111";
        _ipAddressHistory = new ObservableCollection<string>(_settings.IpAddresses);
        
        // Initialize Azure Speech Configuration
        _speechKey = _settings.SpeechKey ?? string.Empty;
        _speechRegion = _settings.SpeechRegion ?? "westeurope";
        _speechSynthesizerRate = _settings.SpeechSynthesizerRate;
        _speechSynthesizerVolume = _settings.SpeechSynthesizerVolume;
        _speakerEngineName = _settings.SpeakerEngineName ?? "AzureCognitiveSpeech";
        _voiceName = _settings.VoiceName ?? "de-DE-KatjaNeural";
        
        // Initialize Application Settings
        _isResetWindowLayoutOnStart = _settings.IsResetWindowLayoutOnStart;
    }

    // Z21 Configuration Change Handlers
    partial void OnCurrentIpAddressChanged(string value)
    {
        _settings.CurrentIpAddress = value;
        
        // Add to history if not already present
        if (!string.IsNullOrWhiteSpace(value) && !_settings.IpAddresses.Contains(value))
        {
            _settings.IpAddresses.Add(value);
            IpAddressHistory.Add(value);
        }
    }

    // Azure Speech Configuration Change Handlers
    partial void OnSpeechKeyChanged(string value)
    {
        _settings.SpeechKey = value;
    }

    partial void OnSpeechRegionChanged(string value)
    {
        _settings.SpeechRegion = value;
    }

    partial void OnSpeechSynthesizerRateChanged(int value)
    {
        _settings.SpeechSynthesizerRate = value;
    }

    partial void OnSpeechSynthesizerVolumeChanged(uint value)
    {
        _settings.SpeechSynthesizerVolume = value;
    }

    partial void OnSpeakerEngineNameChanged(string value)
    {
        _settings.SpeakerEngineName = value;
    }

    partial void OnVoiceNameChanged(string value)
    {
        _settings.VoiceName = value;
    }

    // Application Settings Change Handlers
    partial void OnIsResetWindowLayoutOnStartChanged(bool value)
    {
        _settings.IsResetWindowLayoutOnStart = value;
    }
}

