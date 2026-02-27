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
    /// <summary>
    /// Initializes a new instance of the <see cref="SpeakerEngineConfigurationViewModel"/> class.
    /// </summary>
    /// <param name="model">The speaker engine configuration domain model.</param>
    public SpeakerEngineConfigurationViewModel(SpeakerEngineConfiguration model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>
    /// Gets the underlying speaker engine configuration domain model.
    /// </summary>
    public SpeakerEngineConfiguration Model { get; }

    /// <summary>
    /// Gets or sets the human-readable name of the speaker engine.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the technical type identifier of the speaker engine implementation.
    /// </summary>
    public string Type
    {
        get => Model.Type;
        set => SetProperty(Model.Type, value, Model, (m, v) => m.Type = v);
    }

    /// <summary>
    /// Gets or sets the engine-specific configuration key/value pairs.
    /// </summary>
    public Dictionary<string, string>? Settings
    {
        get => Model.Settings;
        set => SetProperty(Model.Settings, value, Model, (m, v) => m.Settings = v);
    }
}
