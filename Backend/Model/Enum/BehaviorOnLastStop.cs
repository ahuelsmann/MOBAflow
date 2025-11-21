// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.ComponentModel.DataAnnotations;

namespace Moba.Backend.Model.Enum;

public enum BehaviorOnLastStop
{
    [Display(Name = "-")]
    None,
    [Display(Name = "Begin again from fist station")]
    BeginAgainFromFistStop,
    [Display(Name = "Goto journey")]
    GotoJourney,
}