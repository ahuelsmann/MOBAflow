// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.SharedUI.Service;

/// <summary>
/// ViewModel wrapper for Voice model.
/// Represents a specific voice for speech output with prosody rate settings.
/// </summary>
public partial class VoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private Voice model;

    private readonly IUiDispatcher? _dispatcher;

    public VoiceViewModel(Voice model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;
    }

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
