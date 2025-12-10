// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Manager;

using Interface;
using Services;
using Domain;

public class JourneyManagerFactory : IJourneyManagerFactory
{
    private readonly WorkflowService _workflowService;

    public JourneyManagerFactory(WorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    public JourneyManager Create(IZ21 z21, List<Journey> journeys, ActionExecutionContext? context = null)
    {
        // Create a temporary Project wrapper for the journeys
        // In production, this should receive the full Project instead
        var project = new Project { Journeys = journeys };
        return new JourneyManager(z21, project, _workflowService, context);
    }
}