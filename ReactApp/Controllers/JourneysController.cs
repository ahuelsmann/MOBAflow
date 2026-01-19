// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.ReactApp.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// REST API controller for journey operations.
/// Provides CRUD operations and control for automated train journeys.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JourneysController : ControllerBase
{
    private readonly ILogger<JourneysController> _logger;

    // Demo data for validation purposes
    private static readonly List<JourneyDto> DemoJourneys =
    [
        new(1, "Hauptstrecke", 1, ["Hauptbahnhof", "Westbahnhof", "Südbahnhof", "Hauptbahnhof"], false),
        new(2, "Nebenstrecke", 2, ["Dorf", "Fabrik", "Bergwerk", "Dorf"], false),
        new(3, "Schnellzug", 3, ["Hamburg", "Hannover", "Frankfurt", "München"], false),
    ];

    public JourneysController(ILogger<JourneysController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all configured journeys.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(DemoJourneys);
    }

    /// <summary>
    /// Get a specific journey by ID.
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var journey = DemoJourneys.FirstOrDefault(j => j.Id == id);
        if (journey == null)
        {
            return NotFound(new { error = $"Journey with ID {id} not found" });
        }
        return Ok(journey);
    }

    /// <summary>
    /// Start a journey.
    /// </summary>
    [HttpPost("{id}/start")]
    public IActionResult Start(int id)
    {
        var journey = DemoJourneys.FirstOrDefault(j => j.Id == id);
        if (journey == null)
        {
            return NotFound(new { error = $"Journey with ID {id} not found" });
        }

        _logger.LogInformation("Starting journey {JourneyId}: {JourneyName}", id, journey.Name);

        var index = DemoJourneys.FindIndex(j => j.Id == id);
        DemoJourneys[index] = journey with { IsRunning = true };

        return Ok(new { success = true, isRunning = true });
    }

    /// <summary>
    /// Stop a journey.
    /// </summary>
    [HttpPost("{id}/stop")]
    public IActionResult Stop(int id)
    {
        var journey = DemoJourneys.FirstOrDefault(j => j.Id == id);
        if (journey == null)
        {
            return NotFound(new { error = $"Journey with ID {id} not found" });
        }

        _logger.LogInformation("Stopping journey {JourneyId}: {JourneyName}", id, journey.Name);

        var index = DemoJourneys.FindIndex(j => j.Id == id);
        DemoJourneys[index] = journey with { IsRunning = false };

        return Ok(new { success = true, isRunning = false });
    }
}

public record JourneyDto(int Id, string Name, int TrainId, string[] Stations, bool IsRunning);
