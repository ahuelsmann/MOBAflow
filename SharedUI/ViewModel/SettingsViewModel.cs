// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Domain;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.SharedUI.Service;

/// <summary>
/// ViewModel wrapper for Settings model.
/// Provides application-wide configuration for Z21, speech synthesis, and UI behavior.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private Settings model;

    private readonly IUiDispatcher? _dispatcher;

    public SettingsViewModel(Settings model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
    }

    public string? CurrentIpAddress
    {
        get => Model.CurrentIpAddress;
        set => SetProperty(Model.CurrentIpAddress, value, Model, (m, v) => m.CurrentIpAddress = v);
    }

    public string? DefaultPort
    {
        get => Model.DefaultPort;
        set => SetProperty(Model.DefaultPort, value, Model, (m, v) => m.DefaultPort = v);
    }

    public List<string> IpAddresses
    {
        get => Model.IpAddresses;
        set => SetProperty(Model.IpAddresses, value, Model, (m, v) => m.IpAddresses = v);
    }

    public string? SpeechKey
    {
        get => Model.SpeechKey;
        set => SetProperty(Model.SpeechKey, value, Model, (m, v) => m.SpeechKey = v);
    }

    public string? SpeechRegion
    {
        get => Model.SpeechRegion;
        set => SetProperty(Model.SpeechRegion, value, Model, (m, v) => m.SpeechRegion = v);
    }

    public int SpeechSynthesizerRate
    {
        get => Model.SpeechSynthesizerRate;
        set => SetProperty(Model.SpeechSynthesizerRate, value, Model, (m, v) => m.SpeechSynthesizerRate = v);
    }

    public uint SpeechSynthesizerVolume
    {
        get => Model.SpeechSynthesizerVolume;
        set => SetProperty(Model.SpeechSynthesizerVolume, value, Model, (m, v) => m.SpeechSynthesizerVolume = v);
    }

    public string? SpeakerEngineName
    {
        get => Model.SpeakerEngineName;
        set => SetProperty(Model.SpeakerEngineName, value, Model, (m, v) => m.SpeakerEngineName = v);
    }

    public string? VoiceName
    {
        get => Model.VoiceName;
        set => SetProperty(Model.VoiceName, value, Model, (m, v) => m.VoiceName = v);
    }

    public bool IsResetWindowLayoutOnStart
    {
        get => Model.IsResetWindowLayoutOnStart;
        set => SetProperty(Model.IsResetWindowLayoutOnStart, value, Model, (m, v) => m.IsResetWindowLayoutOnStart = v);
    }
}
