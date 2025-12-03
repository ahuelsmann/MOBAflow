// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Enum;

/// <summary>
/// Represents the type of entity currently selected in the Editor.
/// Used by PropertyGrid to determine which properties to display.
/// </summary>
public enum MobaType
{
    None,
    Solution,
    Project,
    Journey,
    Station,
    Workflow,
    Train,
    Locomotive,
    Wagon
}