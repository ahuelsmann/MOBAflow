// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Represents a ViewModel that wraps a domain model for editing.
/// </summary>
/// <typeparam name="TModel">The domain model type.</typeparam>
public interface IViewModelWrapper<TModel> : ISelectableEntity where TModel : class
{
    /// <summary>
    /// Gets the underlying domain model.
    /// </summary>
    TModel Model { get; }
}