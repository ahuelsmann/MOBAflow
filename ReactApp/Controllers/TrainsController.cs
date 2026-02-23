// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.ReactApp.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// REST API controller for train operations.
/// Provides CRUD operations and speed/function control for locomotives.
/// </summary>
[ApiController]
[Route("api/[controller]")]
internal class TrainsController : ControllerBase
{
    private readonly ILogger<TrainsController> _logger;

    // Demo data for validation purposes
    private static readonly List<TrainDto> DemoTrains =
    [
        new(1, "ICE 3", 3, 0, "forward", [true, false, false, false, false]),
        new(2, "BR 101", 101, 0, "forward", [true, true, false, false, false]),
        new(3, "BR 218", 218, 0, "forward", [true, false, true, false, false]),
        new(4, "BR 185", 185, 0, "backward", [true, false, false, false, false]),
    ];

    public TrainsController(ILogger<TrainsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all configured trains.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(DemoTrains);
    }

    /// <summary>
    /// Get a specific train by ID.
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var train = DemoTrains.FirstOrDefault(t => t.Id == id);
        if (train == null)
        {
            return NotFound(new { error = $"Train with ID {id} not found" });
        }
        return Ok(train);
    }

    /// <summary>
    /// Set train speed.
    /// </summary>
    [HttpPost("{id}/speed")]
    public IActionResult SetSpeed(int id, [FromBody] SpeedRequest request)
    {
        var train = DemoTrains.FirstOrDefault(t => t.Id == id);
        if (train == null)
        {
            return NotFound(new { error = $"Train with ID {id} not found" });
        }

        _logger.LogInformation("Setting train {TrainId} speed to {Speed}", id, request.Speed);

        // Update demo data
        var index = DemoTrains.FindIndex(t => t.Id == id);
        DemoTrains[index] = train with { Speed = Math.Clamp(request.Speed, 0, 126) };

        return Ok(new { success = true, speed = DemoTrains[index].Speed });
    }

    /// <summary>
    /// Set train function (lights, sound, etc.).
    /// </summary>
    [HttpPost("{id}/function")]
    public IActionResult SetFunction(int id, [FromBody] FunctionRequest request)
    {
        var train = DemoTrains.FirstOrDefault(t => t.Id == id);
        if (train == null)
        {
            return NotFound(new { error = $"Train with ID {id} not found" });
        }

        if (request.FunctionIndex < 0 || request.FunctionIndex >= train.Functions.Length)
        {
            return BadRequest(new { error = $"Invalid function index {request.FunctionIndex}" });
        }

        _logger.LogInformation("Setting train {TrainId} function {Function} to {State}",
            id, request.FunctionIndex, request.On);

        return Ok(new { success = true });
    }
}

internal record TrainDto(int Id, string Name, int Address, int Speed, string Direction, bool[] Functions);
internal record SpeedRequest(int Speed);
internal record FunctionRequest(int FunctionIndex, bool On);
