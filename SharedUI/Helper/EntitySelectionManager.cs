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
    /// </summary>
    public void SelectEntity<T>(
        T? entity,
        MobaType type,
        T? currentSelected,
        Action<T?> setSelected) where T : class, ISelectableEntity
    {
        // Simply set the entity
        setSelected(entity);

        // Notify PropertyGrid to refresh
        _notifySelectionPropertiesChanged();
    }
}
