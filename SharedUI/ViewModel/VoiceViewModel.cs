// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// ViewModel wrapper for Voice model.
/// </summary>
public sealed class VoiceViewModel : ObservableObject, IViewModelWrapper<Voice>
{
    public VoiceViewModel(Voice model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public Voice Model { get; }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public decimal ProsodyRate
    {
        get => Model.ProsodyRate;
        set => SetProperty(Model.ProsodyRate, value, Model, (m, v) => m.ProsodyRate = v);
    }
}
