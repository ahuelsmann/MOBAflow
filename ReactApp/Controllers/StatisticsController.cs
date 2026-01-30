// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.ReactApp.Controllers;

using Microsoft.AspNetCore.Mvc;

using SharedUI.ViewModel;

/// <summary>
/// REST API controller for track statistics (lap counter).
/// Provides endpoints matching WebAppViewModel.Statistics collection.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly WebAppViewModel _viewModel;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(WebAppViewModel viewModel, ILogger<StatisticsController> logger)
    {
        _viewModel = viewModel;
        _logger = logger;
    }

    /// <summary>
    /// Get all track statistics.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var statistics = _viewModel.Statistics.Select(stat => new TrackStatisticDto(
            InPort: stat.InPort,
            Name: stat.Name,
            Count: stat.Count,
            TargetLapCount: stat.TargetLapCount,
            LastLapTime: stat.LastLapTimeFormatted,
            LastFeedbackTime: stat.LastFeedbackTimeFormatted,
            HasReceivedFirstLap: stat.HasReceivedFirstLap,
            Progress: stat.Progress,
            LapCountFormatted: stat.LapCountFormatted
        )).ToList();

        return Ok(statistics);
    }

    /// <summary>
    /// Get lap counter settings.
    /// </summary>
    [HttpGet("settings")]
    public IActionResult GetSettings()
    {
        return Ok(new LapCounterSettingsDto(
            CountOfFeedbackPoints: _viewModel.CountOfFeedbackPoints,
            GlobalTargetLapCount: _viewModel.GlobalTargetLapCount,
            UseTimerFilter: _viewModel.UseTimerFilter,
            TimerIntervalSeconds: _viewModel.TimerIntervalSeconds
        ));
    }

    /// <summary>
    /// Reset all counters.
    /// </summary>
    [HttpPost("reset")]
    public IActionResult ResetCounters()
    {
        _logger.LogInformation("Resetting all track counters via REST API");

        if (_viewModel.ResetCountersCommand.CanExecute(null))
        {
            _viewModel.ResetCountersCommand.Execute(null);
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Update lap counter settings.
    /// </summary>
    [HttpPut("settings")]
    public IActionResult UpdateSettings([FromBody] LapCounterSettingsDto settings)
    {
        _logger.LogInformation("Updating lap counter settings: FeedbackPoints={Points}, TargetLaps={Target}",
            settings.CountOfFeedbackPoints, settings.GlobalTargetLapCount);

        _viewModel.CountOfFeedbackPoints = settings.CountOfFeedbackPoints;
        _viewModel.GlobalTargetLapCount = settings.GlobalTargetLapCount;
        _viewModel.UseTimerFilter = settings.UseTimerFilter;
        _viewModel.TimerIntervalSeconds = settings.TimerIntervalSeconds;

        return Ok(new { success = true });
    }
}

/// <summary>
/// DTO for track statistics matching InPortStatistic.
/// </summary>
public record TrackStatisticDto(
    int InPort,
    string Name,
    int Count,
    int TargetLapCount,
    string LastLapTime,
    string LastFeedbackTime,
    bool HasReceivedFirstLap,
    double Progress,
    string LapCountFormatted
);

/// <summary>
/// DTO for lap counter settings.
/// </summary>
public record LapCounterSettingsDto(
    int CountOfFeedbackPoints,
    int GlobalTargetLapCount,
    bool UseTimerFilter,
    double TimerIntervalSeconds
);
