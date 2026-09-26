// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moba.MOBApi.Service;

/// <summary>
/// Maps command admission results to the REST contract: 202, 400 or 429.
/// </summary>
internal static class RuntimeCommandAdmissionResultExtensions
{
    internal static IActionResult ToActionResult(this RuntimeCommandAdmissionResult result, ControllerBase controller) =>
        result.Status switch
        {
            RuntimeCommandAdmissionStatus.Accepted => controller.Accepted(),
            RuntimeCommandAdmissionStatus.QueueFull => controller.StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { error = result.Error }),
            _ => controller.BadRequest(new { error = result.Error })
        };
}
