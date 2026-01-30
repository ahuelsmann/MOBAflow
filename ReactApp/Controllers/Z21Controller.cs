// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.ReactApp.Controllers;

using Microsoft.AspNetCore.Mvc;

using SharedUI.ViewModel;

/// <summary>
/// REST API controller for Z21 digital command station operations.
/// Provides endpoints for connecting, controlling track power, and emergency stop.
/// Mirrors WebAppViewModel properties for React frontend.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Z21Controller : ControllerBase
{
    private readonly WebAppViewModel _viewModel;
    private readonly ILogger<Z21Controller> _logger;

    public Z21Controller(WebAppViewModel viewModel, ILogger<Z21Controller> logger)
    {
        _viewModel = viewModel;
        _logger = logger;
    }

    /// <summary>
    /// Get current connection and system status.
    /// Returns all properties needed by React Dashboard (matches WebAppViewModel).
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            // Connection
            isConnected = _viewModel.IsConnected,
            ipAddress = _viewModel.IpAddress,
            statusText = _viewModel.StatusText,
            availableIpAddresses = _viewModel.AvailableIpAddresses.ToList(),

            // System State (matches SystemState in Blazor)
            isTrackPowerOn = _viewModel.IsTrackPowerOn,
            mainCurrent = _viewModel.MainCurrent,
            temperature = _viewModel.Temperature,
            supplyVoltage = _viewModel.SupplyVoltage,
            vccVoltage = _viewModel.VccVoltage,

            // Version Info
            serialNumber = _viewModel.SerialNumber,
            firmwareVersion = _viewModel.FirmwareVersion,
            hardwareType = _viewModel.HardwareType
        });
    }

    /// <summary>
    /// Connect to Z21 command station.
    /// </summary>
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectAsync([FromBody] ConnectRequest request)
    {
        try
        {
            _logger.LogInformation("Connecting to Z21 at {IpAddress}", request.IpAddress);

            // Update IP address in ViewModel
            _viewModel.IpAddress = request.IpAddress;

            // Execute the connect command
            if (_viewModel.ConnectCommand.CanExecute(null))
            {
                await _viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);
            }

            return Ok(new { success = _viewModel.IsConnected, statusText = _viewModel.StatusText });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection failed");
            return Ok(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Disconnect from Z21 command station.
    /// </summary>
    [HttpPost("disconnect")]
    public async Task<IActionResult> DisconnectAsync()
    {
        try
        {
            if (_viewModel.DisconnectCommand.CanExecute(null))
            {
                await _viewModel.DisconnectCommand.ExecuteAsync(null).ConfigureAwait(false);
            }
            return Ok(new { success = true, statusText = _viewModel.StatusText });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disconnect failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Set track power on or off.
    /// </summary>
    [HttpPost("track-power")]
    public async Task<IActionResult> SetTrackPowerAsync([FromBody] TrackPowerRequest request)
    {
        try
        {
            if (_viewModel.SetTrackPowerCommand.CanExecute(request.On))
            {
                await _viewModel.SetTrackPowerCommand.ExecuteAsync(request.On).ConfigureAwait(false);
            }
            return Ok(new { success = true, isTrackPowerOn = _viewModel.IsTrackPowerOn });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Track power command failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Emergency stop - stops all trains immediately by turning off track power.
    /// </summary>
    [HttpPost("emergency-stop")]
    public async Task<IActionResult> EmergencyStopAsync()
    {
        try
        {
            _logger.LogWarning("Emergency stop triggered via REST API");

            // Emergency stop = turn off track power
            if (_viewModel.SetTrackPowerCommand.CanExecute(false))
            {
                await _viewModel.SetTrackPowerCommand.ExecuteAsync(false).ConfigureAwait(false);
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Emergency stop failed");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record ConnectRequest(string IpAddress);
public record TrackPowerRequest(bool On);
