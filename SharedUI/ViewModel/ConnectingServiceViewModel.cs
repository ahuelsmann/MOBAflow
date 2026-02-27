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
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectingServiceViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying connecting service model to wrap.</param>
    public ConnectingServiceViewModel(ConnectingService model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    /// <summary>
    /// Gets the underlying connecting service domain model.
    /// </summary>
    public ConnectingService Model { get; }

    /// <summary>
    /// Gets or sets the display name of the connecting service.
    /// </summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }
}
