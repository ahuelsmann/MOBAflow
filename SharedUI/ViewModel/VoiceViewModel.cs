// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// ViewModel wrapper for <see cref="Voice"/> model.
/// </summary>
public sealed class VoiceViewModel : ObservableObject, IViewModelWrapper<Voice>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceViewModel"/> class.
    /// </summary>
    /// <param name="model">The voice domain model.</param>
    public VoiceViewModel(Voice model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>
    /// Gets the underlying voice domain model.
    /// </summary>
    public Voice Model { get; }

    /// <summary>
    /// Gets or sets the human-readable name of the voice.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the prosody rate (speech speed factor) for this voice.
    /// </summary>
    public decimal ProsodyRate
    {
        get => Model.ProsodyRate;
        set => SetProperty(Model.ProsodyRate, value, Model, (m, v) => m.ProsodyRate = v);
    }
}
