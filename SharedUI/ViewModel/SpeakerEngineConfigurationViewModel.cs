// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// ViewModel wrapper for SpeakerEngineConfiguration model.
/// </summary>
public sealed class SpeakerEngineConfigurationViewModel : ObservableObject, IViewModelWrapper<SpeakerEngineConfiguration>
{
    public SpeakerEngineConfigurationViewModel(SpeakerEngineConfiguration model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public SpeakerEngineConfiguration Model { get; }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public string Type
    {
        get => Model.Type;
        set => SetProperty(Model.Type, value, Model, (m, v) => m.Type = v);
    }

    public Dictionary<string, string>? Settings
    {
        get => Model.Settings;
        set => SetProperty(Model.Settings, value, Model, (m, v) => m.Settings = v);
    }
}
