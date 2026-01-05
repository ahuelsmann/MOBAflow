// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Helper;

using Interface;
using System.Collections.ObjectModel;

/// <summary>
/// Generic helper for Add/Delete operations on entity collections.
/// Eliminates code duplication in CRUD commands.
/// </summary>
public static class EntityEditorHelper
{
    /// <summary>
    /// Generic Add operation for entities.
    /// </summary>
    /// <typeparam name="TModel">Domain model type.</typeparam>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    /// <param name="modelCollection">Collection of domain models (List or ObservableCollection).</param>
    /// <param name="viewModelCollection">Collection of ViewModels.</param>
    /// <param name="createModel">Factory to create new model.</param>
    /// <param name="createViewModel">Factory to create ViewModel from model.</param>
    /// <param name="onAdded">Optional callback after entity is added.</param>
    /// <returns>The created ViewModel.</returns>
    public static TViewModel AddEntity<TModel, TViewModel>(
        ICollection<TModel> modelCollection,
        ObservableCollection<TViewModel> viewModelCollection,
        Func<TModel> createModel,
        Func<TModel, TViewModel> createViewModel,
        Action<TViewModel>? onAdded = null)
        where TModel : class
        where TViewModel : class, IViewModelWrapper<TModel>
    {
        var model = createModel();
        modelCollection.Add(model);

        var viewModel = createViewModel(model);
        viewModelCollection.Add(viewModel);

        onAdded?.Invoke(viewModel);

        return viewModel;
    }

    /// <summary>
    /// Generic Delete operation for entities.
    /// </summary>
    /// <typeparam name="TModel">Domain model type.</typeparam>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    /// <param name="viewModel">ViewModel to delete.</param>
    /// <param name="modelCollection">Collection of domain models (List or ObservableCollection).</param>
    /// <param name="viewModelCollection">Collection of ViewModels.</param>
    /// <param name="onDeleted">Optional callback after entity is deleted.</param>
    public static void DeleteEntity<TModel, TViewModel>(
        TViewModel? viewModel,
        ICollection<TModel> modelCollection,
        ObservableCollection<TViewModel> viewModelCollection,
        Action? onDeleted = null)
        where TModel : class
        where TViewModel : class, IViewModelWrapper<TModel>
    {
        if (viewModel == null) return;

        modelCollection.Remove(viewModel.Model);
        viewModelCollection.Remove(viewModel);

        onDeleted?.Invoke();
    }
}