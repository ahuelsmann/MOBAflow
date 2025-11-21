// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Factory;

using Moba.Backend.Model;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Blazor-specific factory for creating TrainViewModel instances
/// </summary>
public class WebTrainViewModelFactory : ITrainViewModelFactory
{
    public TrainViewModel Create(Train model)
        => new TrainViewModel(model, dispatcher: null);
}
