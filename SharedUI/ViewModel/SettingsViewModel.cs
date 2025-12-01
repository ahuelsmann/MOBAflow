// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Moba.Common.Configuration;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.SharedUI.Service;

/// <summary>
/// ViewModel wrapper for AppSettings configuration.
/// Provides application-wide configuration for Z21, speech synthesis, and UI behavior.
/// Settings are stored in appsettings.json (not in Solution).
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private AppSettings model;

    private readonly IUiDispatcher? _dispatcher;

    public SettingsViewModel(AppSettings model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
    }

    // Z21 Settings
    public string CurrentIpAddress
    {
        get => Model.Z21.CurrentIpAddress;
        set => SetProperty(Model.Z21.CurrentIpAddress, value, Model.Z21, (m, v) => m.CurrentIpAddress = v);
    }

    public string DefaultPort
    {
        get => Model.Z21.DefaultPort;
        set => SetProperty(Model.Z21.DefaultPort, value, Model.Z21, (m, v) => m.DefaultPort = v);
    }

    public List<string> RecentIpAddresses
    {
        get => Model.Z21.RecentIpAddresses;
        set => SetProperty(Model.Z21.RecentIpAddresses, value, Model.Z21, (m, v) => m.RecentIpAddresses = v);
    }

    // Speech Settings
    public string SpeechKey
    {
        get => Model.Speech.Key;
        set => SetProperty(Model.Speech.Key, value, Model.Speech, (m, v) => m.Key = v);
    }

    public string SpeechRegion
    {
        get => Model.Speech.Region;
        set => SetProperty(Model.Speech.Region, value, Model.Speech, (m, v) => m.Region = v);
    }

    public int SpeechSynthesizerRate
    {
        get => Model.Speech.Rate;
        set => SetProperty(Model.Speech.Rate, value, Model.Speech, (m, v) => m.Rate = v);
    }

    public uint SpeechSynthesizerVolume
    {
        get => Model.Speech.Volume;
        set => SetProperty(Model.Speech.Volume, value, Model.Speech, (m, v) => m.Volume = v);
    }

    public string? SpeakerEngineName
    {
        get => Model.Speech.SpeakerEngineName;
        set => SetProperty(Model.Speech.SpeakerEngineName, value, Model.Speech, (m, v) => m.SpeakerEngineName = v);
    }

    public string? VoiceName
    {
        get => Model.Speech.VoiceName;
        set => SetProperty(Model.Speech.VoiceName, value, Model.Speech, (m, v) => m.VoiceName = v);
    }

    // Application Settings
    public bool IsResetWindowLayoutOnStart
    {
        get => Model.Application.ResetWindowLayoutOnStart;
        set => SetProperty(Model.Application.ResetWindowLayoutOnStart, value, Model.Application, (m, v) => m.ResetWindowLayoutOnStart = v);
    }

    public bool AutoLoadLastSolution
    {
        get => Model.Application.AutoLoadLastSolution;
        set => SetProperty(Model.Application.AutoLoadLastSolution, value, Model.Application, (m, v) => m.AutoLoadLastSolution = v);
    }

    public string? LastSolutionPath
    {
        get => Model.Application.LastSolutionPath;
        set => SetProperty(Model.Application.LastSolutionPath, value, Model.Application, (m, v) => m.LastSolutionPath = v);
    }

    // City Library Settings
    public string CityLibraryFilePath
    {
        get => Model.CityLibrary.FilePath;
        set => SetProperty(Model.CityLibrary.FilePath, value, Model.CityLibrary, (m, v) => m.FilePath = v);
    }

    public bool CityLibraryAutoReload
    {
        get => Model.CityLibrary.AutoReload;
        set => SetProperty(Model.CityLibrary.AutoReload, value, Model.CityLibrary, (m, v) => m.AutoReload = v);
    }
}
