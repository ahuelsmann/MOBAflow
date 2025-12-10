// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Interface;

using Manager;
using Services;
using Domain;

public interface IJourneyManagerFactory
{
    /// <summary>
    /// Creates a JourneyManager for the specified project.
    /// </summary>
    /// <param name="z21">Z21 command station interface</param>
    /// <param name="project">Project containing journeys, stations, and workflows</param>
    /// <param name="context">Optional execution context for action execution</param>
    JourneyManager Create(IZ21 z21, Project project, ActionExecutionContext? context = null);
}