// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// ViewModel wrapper for ConnectingService model.
/// </summary>
public sealed class ConnectingServiceViewModel : ObservableObject, IViewModelWrapper<ConnectingService>
{
    public ConnectingServiceViewModel(ConnectingService model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public ConnectingService Model { get; }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }
}
