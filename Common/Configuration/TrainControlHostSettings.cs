// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Configuration;

/// <summary>
/// Host-specific train control UI state (not shared between MOBAflow and MOBAsmart).
/// </summary>
public class TrainControlHostSettings
{
    /// <summary>
    /// ID of the locomotive last selected in this client's locomotive picker.
    /// </summary>
    public Guid? SelectedLocomotiveFromProjectId { get; set; }
}
