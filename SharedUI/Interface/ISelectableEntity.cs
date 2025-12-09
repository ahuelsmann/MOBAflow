// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

using Moba.SharedUI.Enum;

/// <summary>
/// Represents an entity that can be selected in the Editor UI.
/// </summary>
public interface ISelectableEntity
{
    /// <summary>
    /// Gets the MobaType of this entity for selection management.
    /// </summary>
    MobaType EntityType { get; }
    
    /// <summary>
    /// Gets the display name of the entity.
    /// </summary>
    string Name { get; }
}