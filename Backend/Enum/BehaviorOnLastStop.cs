using System.ComponentModel.DataAnnotations;

namespace Moba.Backend.Enum;

public enum BehaviorOnLastStop
{
    [Display(Name = "-")]
    None,
    [Display(Name = "Begin again from fist station")]
    BeginAgainFromFistStop,
    [Display(Name = "Goto journey")]
    GotoJourney,
}