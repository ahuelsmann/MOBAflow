// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

/// <summary>
/// Indicates which entity type received an uploaded photo assignment from the REST/PhotoHub pipeline.
/// </summary>
public enum PhotoAssignmentTarget
{
    None,
    Locomotive,
    PassengerWagon,
    GoodsWagon
}
