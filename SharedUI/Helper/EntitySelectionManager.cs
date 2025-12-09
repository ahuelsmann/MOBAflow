// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Helper;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Interface;

/// <summary>
/// Generic helper for managing entity selection with hierarchy support.
/// Simplified: Just sets the entity and notifies changes.
/// ContentControl + DataTemplateSelector handles the rest automatically.
/// </summary>
public class EntitySelectionManager : ObservableObject
{
    private readonly Action _notifySelectionPropertiesChanged;

    public EntitySelectionManager(Action notifySelectionPropertiesChanged)
    {
        _notifySelectionPropertiesChanged = notifySelectionPropertiesChanged;
    }

    /// <summary>
    /// Generic selection logic: Set entity and notify.
    /// The ContentControl automatically picks the right template based on type.
    /// Supports re-selection callback for forcing UI refresh when selecting same item.
    /// Clears child selections to maintain correct hierarchy (e.g., selecting Journey clears Station).
    /// </summary>
    public void SelectEntity<T>(
        T? entity,
        MobaType type,
        T? currentSelected,
        Action<T?> setSelected,
        Action? onReselect = null,
        Action? clearChildSelections = null) where T : class, ISelectableEntity
    {
        // Check if this is a re-selection (same entity clicked again)
        bool isReselection = EqualityComparer<T>.Default.Equals(entity, currentSelected);

        // Clear child selections BEFORE setting new selection (maintain hierarchy)
        clearChildSelections?.Invoke();

        // Set the entity (even if same, setter might do additional work)
        setSelected(entity);

        // Notify PropertyGrid to refresh
        _notifySelectionPropertiesChanged();

        // If re-selection and callback provided, execute it
        if (isReselection && onReselect != null)
        {
            onReselect();
        }
    }
}
