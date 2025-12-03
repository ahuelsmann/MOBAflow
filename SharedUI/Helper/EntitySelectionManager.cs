// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Helper;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Interface;

/// <summary>
/// Generic helper for managing entity selection with hierarchy support.
/// Eliminates code duplication in Select commands.
/// </summary>
public class EntitySelectionManager : ObservableObject
{
    private readonly Action<MobaType> _clearOtherSelections;
    private readonly Action _notifySelectionPropertiesChanged;

    public EntitySelectionManager(
        Action<MobaType> clearOtherSelections,
        Action notifySelectionPropertiesChanged)
    {
        _clearOtherSelections = clearOtherSelections;
        _notifySelectionPropertiesChanged = notifySelectionPropertiesChanged;
    }

    /// <summary>
    /// Generic selection logic that works for all entity types.
    /// Handles clearing other selections, setting entity type, and notifying changes.
    /// </summary>
    /// <param name="entity">Entity to select (can be null to deselect).</param>
    /// <param name="type">MobaType of the entity.</param>
    /// <param name="currentSelected">Current selected entity for comparison.</param>
    /// <param name="setSelected">Action to set the selected entity.</param>
    public void SelectEntity<T>(
        T? entity,
        MobaType type,
        T? currentSelected,
        Action<T?> setSelected) where T : class, ISelectableEntity
    {
        if (entity != null)
        {
            _clearOtherSelections(type);
        }

        var wasAlreadySelected = ReferenceEquals(currentSelected, entity);
        setSelected(entity);

        // Always notify if same entity clicked again (for PropertyGrid refresh)
        if (wasAlreadySelected || entity != null)
        {
            _notifySelectionPropertiesChanged();
        }
    }
}
